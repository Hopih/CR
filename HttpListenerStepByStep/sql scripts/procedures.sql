CREATE OR REPLACE PROCEDURE public.create_board(
IN p_title varchar(120), OUT p_id uuid
)
LANGUAGE plpgsql AS 
$$
    BEGIN
        INSERT INTO public.boards(title)
        VALUES (p_title)
        RETURNING id INTO p_id;
    END;
$$;
-- NULL занимает позицию OUT-параметра; результат CALL содержит p_id.
CALL public.create_board('Доска занятия', NULL);

CREATE OR REPLACE PROCEDURE public.update_sticker(
IN p_board_id uuid, IN p_sticker_id uuid,
IN p_text varchar(2000), IN p_x integer, IN p_y integer,
IN p_width integer, IN p_height integer,
OUT p_found boolean
)
LANGUAGE plpgsql AS $$
BEGIN
UPDATE public.stickers AS s
SET text = p_text, x = p_x, y = p_y,
    width = p_width, height = p_height
WHERE s.id = p_sticker_id AND s.board_id = p_board_id;
p_found := FOUND;
END;
$$;