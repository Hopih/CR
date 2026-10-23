BEGIN;
CREATE TABLE public.users (
                              id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                              login varchar(40) NOT NULL,
                              display_name varchar(80) NOT NULL CHECK (length(btrim(display_name)) > 0),
                              password_hash text NOT NULL,
                              created_at timestamptz NOT NULL DEFAULT now(),
                              CONSTRAINT uq_users_login UNIQUE(login),
                              CONSTRAINT ck_users_login CHECK(login ~ '^[a-z0-9_]{3,40}$')
    );
ALTER TABLE public.boards ADD COLUMN owner_user_id uuid NULL;
ALTER TABLE public.boards ADD CONSTRAINT fk_boards_owner
    FOREIGN KEY(owner_user_id) REFERENCES public.users(id) ON DELETE RESTRICT;
CREATE INDEX ix_boards_owner ON public.boards(owner_user_id);
CREATE OR REPLACE PROCEDURE public.create_user(
IN p_login varchar, IN p_display_name varchar,
IN p_password_hash text, OUT p_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
INSERT INTO public.users(login, display_name, password_hash)
VALUES(p_login, p_display_name, p_password_hash) RETURNING id INTO p_id;
END;
$$;
CREATE OR REPLACE FUNCTION public.get_user_profile(p_id uuid)
RETURNS TABLE(id uuid, login varchar, display_name varchar)
LANGUAGE sql STABLE AS $$
SELECT u.id, u.login, u.display_name FROM public.users u WHERE u.id = p_id;
$$;
CREATE OR REPLACE PROCEDURE public.assign_board_owner(
IN p_board_id uuid, IN p_user_id uuid, OUT p_found boolean)
LANGUAGE plpgsql AS $$
BEGIN
UPDATE public.boards SET owner_user_id = p_user_id
WHERE id = p_board_id AND owner_user_id IS NULL;
p_found := FOUND;
END;
$$;
COMMIT;


BEGIN;
ALTER TABLE public.users ADD COLUMN role varchar(16) NOT NULL DEFAULT 'User';
ALTER TABLE public.users ADD CONSTRAINT ck_users_role CHECK(role IN ('User', 'Admin'));
CREATE OR REPLACE FUNCTION public.get_login_user(p_login varchar)
RETURNS TABLE(id uuid, login varchar, display_name varchar, password_hash text, role varchar)
LANGUAGE sql STABLE AS $$
SELECT u.id,u.login,u.display_name,u.password_hash,u.role
FROM public.users u WHERE u.login = p_login;
$$;
CREATE OR REPLACE FUNCTION public.get_session_user(p_id uuid)
RETURNS TABLE(id uuid, login varchar, display_name varchar, password_hash text, role varchar)
LANGUAGE sql STABLE AS $$
SELECT u.id,u.login,u.display_name,u.password_hash,u.role
FROM public.users u WHERE u.id = p_id;
$$;
CREATE OR REPLACE PROCEDURE public.update_user_hash(
IN p_id uuid, IN p_old_hash text, IN p_hash text, OUT p_found boolean)
LANGUAGE plpgsql AS $$
BEGIN
UPDATE public.users SET password_hash = p_hash
WHERE id = p_id AND password_hash = p_old_hash;
p_found := FOUND;
END;
$$;
CREATE OR REPLACE PROCEDURE public.set_user_role(
IN p_id uuid, IN p_role varchar, OUT p_found boolean)
LANGUAGE plpgsql AS $$
BEGIN
UPDATE public.users SET role = p_role WHERE id = p_id;
p_found := FOUND;
END;
$$;
CREATE OR REPLACE FUNCTION public.can_access_board(
p_board_id uuid, p_user_id uuid, p_is_admin boolean)
RETURNS boolean LANGUAGE sql STABLE AS $$
SELECT EXISTS(SELECT 1 FROM public.boards b
              WHERE b.id = p_board_id AND (b.owner_user_id = p_user_id OR p_is_admin));
$$;
CREATE OR REPLACE FUNCTION public.get_user_boards(p_user_id uuid)
RETURNS TABLE(id uuid, title varchar)
LANGUAGE sql STABLE AS $$
SELECT b.id,b.title FROM public.boards b
WHERE b.owner_user_id = p_user_id ORDER BY b.created_at,b.id;
$$;
CREATE OR REPLACE PROCEDURE public.create_owned_board(
IN p_title varchar, IN p_owner_id uuid, OUT p_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
INSERT INTO public.boards(title, owner_user_id)
VALUES(p_title,p_owner_id) RETURNING id INTO p_id;
END;
$$;
COMMIT;
