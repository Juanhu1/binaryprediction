CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE markets (
    id uuid NOT NULL,
    question character varying(500) NOT NULL,
    slug character varying(200) NOT NULL,
    active boolean NOT NULL,
    closed boolean NOT NULL,
    liquidity numeric(18,2) NOT NULL,
    volume numeric(18,2) NOT NULL,
    probability numeric NOT NULL,
    end_date timestamp with time zone,
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_markets PRIMARY KEY (id)
);

CREATE TABLE ai_analyses (
    id uuid NOT NULL,
    market_id uuid NOT NULL,
    market_probability numeric NOT NULL,
    estimated_probability numeric(9,6) NOT NULL,
    edge numeric NOT NULL,
    confidence numeric(9,6) NOT NULL,
    summary text NOT NULL,
    key_reasons_json text NOT NULL,
    risk_factors_json text NOT NULL,
    model_name text NOT NULL,
    prompt_version text NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_ai_analyses PRIMARY KEY (id),
    CONSTRAINT fk_ai_analyses_markets_market_id FOREIGN KEY (market_id) REFERENCES markets (id) ON DELETE CASCADE
);

CREATE TABLE alerts (
    id uuid NOT NULL,
    market_id uuid NOT NULL,
    message character varying(1000) NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_alerts PRIMARY KEY (id),
    CONSTRAINT fk_alerts_markets_market_id FOREIGN KEY (market_id) REFERENCES markets (id) ON DELETE CASCADE
);

CREATE TABLE market_snapshots (
    id uuid NOT NULL,
    market_id uuid NOT NULL,
    probability numeric(9,6) NOT NULL,
    liquidity numeric(18,2) NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_market_snapshots PRIMARY KEY (id),
    CONSTRAINT fk_market_snapshots_markets_market_id FOREIGN KEY (market_id) REFERENCES markets (id) ON DELETE CASCADE
);

CREATE INDEX ix_ai_analyses_market_id_created_at_utc ON ai_analyses (market_id, created_at_utc);

CREATE INDEX ix_alerts_market_id_created_at_utc ON alerts (market_id, created_at_utc);

CREATE INDEX ix_market_snapshots_market_id_created_at_utc ON market_snapshots (market_id, created_at_utc);

CREATE INDEX ix_markets_active_closed ON markets (active, closed);

CREATE UNIQUE INDEX ix_markets_slug ON markets (slug);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260524043705_InitialCreate', '9.0.0');

ALTER TABLE markets ADD category integer NOT NULL DEFAULT 0;

ALTER TABLE markets ADD eligible_for_analysis boolean NOT NULL DEFAULT FALSE;

ALTER TABLE markets ADD last_quality_evaluation_utc timestamp with time zone;

ALTER TABLE markets ADD quality_score integer;

ALTER TABLE markets ADD rejection_reason text;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260525081628_Day5_MarketFiltering', '9.0.0');

ALTER TABLE markets ADD estimated_resolution_date_utc timestamp with time zone;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260526055824_Day5_ResolutionDate', '9.0.0');


                CREATE VIEW eligible_markets_view AS
                SELECT 
                    id, question, category, quality_score, probability, 
                    liquidity, volume, end_date, estimated_resolution_date_utc, 
                    created_at_utc
                FROM markets
                WHERE eligible_for_analysis = true;
            

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260526064629_Day5_EligibleMarketsView', '9.0.0');

CREATE TABLE market_analysis_queue (
    id uuid NOT NULL,
    market_id uuid NOT NULL,
    status text NOT NULL,
    priority integer NOT NULL,
    retry_count integer NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    started_at_utc timestamp with time zone,
    completed_at_utc timestamp with time zone,
    last_error text,
    CONSTRAINT pk_market_analysis_queue PRIMARY KEY (id),
    CONSTRAINT fk_market_analysis_queue_markets_market_id FOREIGN KEY (market_id) REFERENCES markets (id) ON DELETE CASCADE
);

CREATE INDEX ix_market_analysis_queue_created_at_utc ON market_analysis_queue (created_at_utc);

CREATE UNIQUE INDEX ix_market_analysis_queue_market_id_pending_unique ON market_analysis_queue (market_id) WHERE status = 'Pending';

CREATE INDEX ix_market_analysis_queue_priority ON market_analysis_queue (priority);

CREATE INDEX ix_market_analysis_queue_status ON market_analysis_queue (status);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260526065943_Day6_AnalysisQueue', '9.0.0');

DROP INDEX ix_market_analysis_queue_market_id_pending_unique;

CREATE UNIQUE INDEX ux_market_analysis_queue_active ON market_analysis_queue (market_id) WHERE status IN ('Pending', 'Processing');

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260605070442_UpdateActiveQueueIndex', '9.0.0');

COMMIT;

