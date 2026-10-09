CREATE OR REPLACE FUNCTION public.get_board(p_board_id uuid)
RETURNS TABLE(id uuid, title varchar(120), created_at timestamptz)
LANGUAGE sql STABLE AS 
$$
    SELECT b.id, b.title, b.created_at
    FROM public.boards AS b
    WHERE b.id = p_board_id;
$$;
CREATE OR REPLACE FUNCTION public.get_board_stickers(p_board_id uuid)
RETURNS TABLE(
id uuid, board_id uuid, text varchar(2000),
x integer, y integer, width integer, height integer,
created_at timestamptz
)
LANGUAGE sql STABLE AS 
$$
    SELECT s.id, s.board_id, s.text, s.x, s.y,
       s.width, s.height, s.created_at
    FROM public.stickers AS s
    WHERE s.board_id = p_board_id
    ORDER BY s.created_at, s.id;
$$;