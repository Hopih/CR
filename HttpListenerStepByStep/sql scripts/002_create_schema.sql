BEGIN;
CREATE TABLE public.boards (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    title varchar(120) NOT NULL CHECK (length(btrim(title)) > 0),
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE public.stickers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id uuid NOT NULL REFERENCES public.boards(id) ON DELETE CASCADE,
    text varchar(2000) NOT NULL DEFAULT '',
    x integer NOT NULL CHECK (x BETWEEN 0 AND 10000),
    y integer NOT NULL CHECK (y BETWEEN 0 AND 10000),
    width integer NOT NULL DEFAULT 200 CHECK (width BETWEEN 80 AND 10000),
    height integer NOT NULL DEFAULT 200 CHECK (height BETWEEN 80 AND 10000),
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_stickers_board_id ON public.stickers(board_id);
COMMIT;