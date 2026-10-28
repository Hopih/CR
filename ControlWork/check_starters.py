"""Integration smoke checks. Run after dotnet build, with both ports free.

Uses only Python's standard library. The rates check needs internet access.
"""
import concurrent.futures
import json
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parent
processes = []


def start(project, port):
    dll = ROOT / project / "bin" / "Debug" / "net10.0" / f"{project}.dll"
    process = subprocess.Popen(
        ["dotnet", str(dll)], cwd=ROOT,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0,
    )
    processes.append(process)
    for _ in range(100):
        if process.poll() is not None:
            raise RuntimeError(f"{project} exited: {process.returncode}")
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=.2):
                return
        except OSError:
            time.sleep(.05)
    raise RuntimeError(f"{project} did not start")


def request(path, method="GET"):
    req = urllib.request.Request("http://localhost:8081" + path, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=20)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, response.headers, response.read()


def exchange(stream, command):
    stream.write((command + "\n").encode())
    return stream.readline().decode().strip()


try:
    start("CurrencyWeb", 8081)
    start("NumberGuessServer", 5050)
    for path in ("/", "/index.html", "/app.js", "/styles.css"):
        status, _, body = request(path)
        assert status == 200 and body, path
    assert request("/missing")[0] == 404
    status, headers, _ = request("/api/rates", "POST")
    assert status == 405 and headers["Allow"] == "GET"
    status, headers, body = request("/api/rates")
    assert status == 200, body.decode()
    rates = json.loads(body)
    assert "application/json" in headers["Content-Type"]
    assert rates["date"] and len(rates["currencies"]) > 10
    assert any(c["charCode"] == "USD" for c in rates["currencies"])
    assert all(c["nominal"] > 0 and c["value"] > 0 for c in rates["currencies"])
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
        assert all(result[2] == body for result in pool.map(request, ["/api/rates"] * 5))
    print("PASS HTTP: static files, 404, 405, live JSON rates, concurrent cached responses")

    with socket.create_connection(("127.0.0.1", 5050), timeout=5) as first, \
            socket.create_connection(("127.0.0.1", 5050), timeout=5) as second:
        with first.makefile("rwb", buffering=0) as a, second.makefile("rwb", buffering=0) as b:
            assert exchange(a, "GUESS 50") == "ERROR NOT_STARTED"
            for command in ("GUESS 0", "GUESS 101", "GUESS abc", "GUESS", "GUESS 2 3"):
                assert exchange(a, command) == "ERROR INVALID_NUMBER"
            assert exchange(a, "START extra") == "ERROR INVALID_ARGUMENTS"
            assert exchange(a, "") == "ERROR UNKNOWN_COMMAND"
            assert exchange(a, "HELLO") == "ERROR UNKNOWN_COMMAND"
            assert exchange(a, "START\r") == "OK"
            # A multibyte UTF-8 character can also be split between receives.
            letter = "Я".encode("utf-8")
            a.write(letter[:1])
            a.write(letter[1:] + b"\nSTART\n")
            assert a.readline().strip() == b"ERROR UNKNOWN_COMMAND"
            assert a.readline().strip() == b"OK"
            # One command split between sends; two commands in one send.
            a.write(b"STA")
            a.write(b"RT\nGUESS 50\n")
            assert a.readline().strip() == b"OK"
            assert a.readline().strip() in (b"LESS", b"GREATER", b"WIN")
            assert exchange(b, "GUESS 50") == "ERROR NOT_STARTED"
            assert exchange(b, "start") == "OK"
            assert exchange(a, "START") == "OK"
            low, high = 1, 100
            for _ in range(7):
                guess = (low + high) // 2
                answer = exchange(a, f"GUESS {guess}")
                if answer == "WIN":
                    break
                assert answer in ("GREATER", "LESS"), answer
                if answer == "GREATER":
                    low = guess + 1
                else:
                    high = guess - 1
            else:
                raise AssertionError("Could not win within seven guesses")
            assert exchange(a, "GUESS 50") == "ERROR NOT_STARTED"
            assert exchange(b, "GUESS 50") in ("GREATER", "LESS", "WIN")
            assert exchange(a, "QUIT extra") == "ERROR INVALID_ARGUMENTS"
            assert exchange(a, "QUIT") == "BYE"
            assert a.readline() == b""
    with socket.create_connection(("127.0.0.1", 5050), timeout=5) as oversized:
        with oversized.makefile("rwb", buffering=0) as stream:
            stream.write(b"x" * 1025)
            assert stream.readline().strip() == b"ERROR LINE_TOO_LONG"
            assert stream.readline() == b""
    with socket.create_connection(("127.0.0.1", 5050), timeout=5) as unfinished:
        unfinished.sendall(b"STA")
        unfinished.shutdown(socket.SHUT_WR)
        assert unfinished.recv(512) == b""
    print("PASS TCP: errors, framing, independent sessions, hints, win, restart, quit")

    client_dll = ROOT / "NumberGuessClient/bin/Debug/net10.0/NumberGuessClient.dll"
    commands = "start\n" + "".join(f"{n}\n" for n in range(1, 101)) + "quit\n"
    client = subprocess.run(
        ["dotnet", str(client_dll)], input=commands, capture_output=True,
        encoding="utf-8", timeout=15,
        creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0,
    )
    assert client.returncode == 0, client.stderr
    assert "Вы угадали!" in client.stdout and "До встречи!" in client.stdout
    print("PASS client: console input, real game, victory and clean exit")
finally:
    for process in processes:
        if process.poll() is None:
            process.terminate()
        process.wait(timeout=10)
