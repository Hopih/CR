document.getElementById("userForm").addEventListener("submit", async (event)=>{
    event.preventDefault();
    var myFormData = new FormData(event.target);

    var user = {};
    myFormData.forEach((value, key) => (user[key] = value));
    console.log(user);

    let response = await fetch('/registration', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json;charset=utf-8'
        },
        body: JSON.stringify(user)
    });

    if (response.ok) {
        window.resultMessage.innerText = "Успешное сохранение клиента. " +
            "Через пару секунд вы будете перенаправлены на главную страницу";
        setTimeout(function () {
            window.location.href = "/home";
        }, 2000);
    }
    else {
        window.resultMessage.innerText = `Увы, во время сохранение пользователя произошла непредвиденная ошибка.` +
            `Информация по ошибке ${result.message}`;
    }
});
