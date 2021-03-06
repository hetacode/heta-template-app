CREATE DATABASE "template-func-commits-history"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

\c "template-func-commits-history";

CREATE TABLE IF NOT EXISTS public.commits
(
    commit_hash character varying(50) COLLATE pg_catalog."default" NOT NULL,
    repo_hash character varying(65) COLLATE pg_catalog."default" NOT NULL,
    created_at timestamp without time zone NOT NULL,
    CONSTRAINT commits_pkey PRIMARY KEY (commit_hash)
)

TABLESPACE pg_default;

ALTER TABLE public.commits
    OWNER to postgres;