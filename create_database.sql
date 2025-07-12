
-- CREATE DATABASE marketassetsdb;


-- \c marketassetsdb

CREATE TABLE IF NOT EXISTS assets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    symbol VARCHAR(20) NOT NULL,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL,
    instrumentid VARCHAR(100),
    provider VARCHAR(50),
    UNIQUE(symbol, provider)
);


CREATE TABLE IF NOT EXISTS prices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    value NUMERIC(18,6) NOT NULL,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);


CREATE INDEX IF NOT EXISTS idx_prices_asset_id ON prices(asset_id);
CREATE INDEX IF NOT EXISTS idx_prices_updated_at ON prices(updated_at); 