-- This script runs automatically when the PostgreSQL container starts for the
-- first time. It creates separate databases for each service's data.
--
-- WHY separate databases per service?
-- In microservices, each service owns its data. This enforces:
--   1. Service autonomy - services don't share tables
--   2. Independent scaling - each DB can be scaled separately
--   3. Schema isolation - changes in one service don't break others

-- catalog_db: Stores product catalog data (name, price, description)
-- Used by CatalogService for product CRUD operations
CREATE DATABASE catalog_db;

-- order_read_db: Stores the CQRS read model for orders
-- Used by OrderService's query side (denormalized views for fast reads)
-- The write side uses EventStoreDB, this is the materialized view
CREATE DATABASE order_read_db;

-- saga_state_db: Stores saga state machine persistence
-- Used by MassTransit to track the state of distributed transactions
-- Each saga instance (order workflow) has a row here with its current state
CREATE DATABASE saga_state_db;

-- inventory_db: Stores stock levels and reservation records
-- Used by InventoryService for stock management
CREATE DATABASE inventory_db;

-- Grant all privileges (for development only!)
-- In production, each service would have its own limited user
GRANT ALL PRIVILEGES ON DATABASE catalog_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE order_read_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE saga_state_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE inventory_db TO postgres;
