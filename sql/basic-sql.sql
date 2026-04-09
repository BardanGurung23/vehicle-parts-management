BEGIN;

CREATE TABLE IF NOT EXISTS roles (
    role_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS users (
    user_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    role_id INTEGER NOT NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS customers (
    customer_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id INTEGER NOT NULL UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
    address TEXT,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS vehicles (
    vehicle_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    vehicle_number VARCHAR(30) NOT NULL UNIQUE,
    brand VARCHAR(80) NOT NULL,
    model VARCHAR(80) NOT NULL,
    year INTEGER,
    engine_number VARCHAR(100),
    chassis_number VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT vehicles_year_check CHECK (year IS NULL OR year BETWEEN 1900 AND 2100)
);

CREATE TABLE IF NOT EXISTS vendors (
    vendor_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    vendor_name VARCHAR(150) NOT NULL,
    contact_person VARCHAR(150),
    phone_number VARCHAR(20),
    email VARCHAR(150),
    address TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS part_categories (
    part_category_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    category_name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT
);

CREATE TABLE IF NOT EXISTS parts (
    part_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    part_category_id INTEGER REFERENCES part_categories(part_category_id) ON DELETE SET NULL,
    part_number VARCHAR(50) NOT NULL UNIQUE,
    part_name VARCHAR(150) NOT NULL,
    description TEXT,
    unit_price NUMERIC(12, 2) NOT NULL,
    cost_price NUMERIC(12, 2) NOT NULL,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    reorder_level INTEGER NOT NULL DEFAULT 10,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT parts_unit_price_check CHECK (unit_price >= 0),
    CONSTRAINT parts_cost_price_check CHECK (cost_price >= 0),
    CONSTRAINT parts_stock_quantity_check CHECK (stock_quantity >= 0),
    CONSTRAINT parts_reorder_level_check CHECK (reorder_level >= 0)
);

CREATE TABLE IF NOT EXISTS purchase_invoices (
    purchase_invoice_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    vendor_id INTEGER NOT NULL REFERENCES vendors(vendor_id) ON DELETE RESTRICT,
    created_by_user_id INTEGER NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    invoice_number VARCHAR(50) NOT NULL UNIQUE,
    invoice_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    total_amount NUMERIC(12, 2) NOT NULL DEFAULT 0,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft',
    CONSTRAINT purchase_invoices_total_amount_check CHECK (total_amount >= 0)
);

CREATE TABLE IF NOT EXISTS purchase_invoice_items (
    purchase_invoice_item_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    purchase_invoice_id INTEGER NOT NULL REFERENCES purchase_invoices(purchase_invoice_id) ON DELETE CASCADE,
    part_id INTEGER NOT NULL REFERENCES parts(part_id) ON DELETE RESTRICT,
    quantity INTEGER NOT NULL,
    unit_cost NUMERIC(12, 2) NOT NULL,
    line_total NUMERIC(12, 2) NOT NULL,
    CONSTRAINT purchase_invoice_items_quantity_check CHECK (quantity > 0),
    CONSTRAINT purchase_invoice_items_unit_cost_check CHECK (unit_cost >= 0),
    CONSTRAINT purchase_invoice_items_line_total_check CHECK (line_total >= 0)
);

CREATE TABLE IF NOT EXISTS sales_invoices (
    sales_invoice_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE RESTRICT,
    vehicle_id INTEGER REFERENCES vehicles(vehicle_id) ON DELETE SET NULL,
    created_by_user_id INTEGER NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    invoice_number VARCHAR(50) NOT NULL UNIQUE,
    invoice_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    subtotal NUMERIC(12, 2) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(12, 2) NOT NULL DEFAULT 0,
    total_amount NUMERIC(12, 2) NOT NULL DEFAULT 0,
    payment_status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    due_date TIMESTAMPTZ,
    CONSTRAINT sales_invoices_subtotal_check CHECK (subtotal >= 0),
    CONSTRAINT sales_invoices_discount_amount_check CHECK (discount_amount >= 0),
    CONSTRAINT sales_invoices_total_amount_check CHECK (total_amount >= 0)
);

CREATE TABLE IF NOT EXISTS sales_invoice_items (
    sales_invoice_item_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sales_invoice_id INTEGER NOT NULL REFERENCES sales_invoices(sales_invoice_id) ON DELETE CASCADE,
    part_id INTEGER NOT NULL REFERENCES parts(part_id) ON DELETE RESTRICT,
    quantity INTEGER NOT NULL,
    unit_price NUMERIC(12, 2) NOT NULL,
    line_total NUMERIC(12, 2) NOT NULL,
    CONSTRAINT sales_invoice_items_quantity_check CHECK (quantity > 0),
    CONSTRAINT sales_invoice_items_unit_price_check CHECK (unit_price >= 0),
    CONSTRAINT sales_invoice_items_line_total_check CHECK (line_total >= 0)
);

CREATE TABLE IF NOT EXISTS appointments (
    appointment_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    vehicle_id INTEGER NOT NULL REFERENCES vehicles(vehicle_id) ON DELETE CASCADE,
    appointment_date TIMESTAMPTZ NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS reviews (
    review_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    appointment_id INTEGER NOT NULL UNIQUE REFERENCES appointments(appointment_id) ON DELETE CASCADE,
    rating INTEGER NOT NULL,
    comment TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT reviews_rating_check CHECK (rating BETWEEN 1 AND 5)
);

CREATE TABLE IF NOT EXISTS part_requests (
    part_request_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    vehicle_id INTEGER REFERENCES vehicles(vehicle_id) ON DELETE SET NULL,
    requested_part_name VARCHAR(150) NOT NULL,
    request_details TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    requested_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS predictive_alerts (
    predictive_alert_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    vehicle_id INTEGER NOT NULL REFERENCES vehicles(vehicle_id) ON DELETE CASCADE,
    part_id INTEGER REFERENCES parts(part_id) ON DELETE SET NULL,
    alert_message TEXT NOT NULL,
    risk_level VARCHAR(30) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status VARCHAR(30) NOT NULL DEFAULT 'Active'
);

CREATE INDEX IF NOT EXISTS ix_users_full_name ON users(full_name);
CREATE INDEX IF NOT EXISTS ix_users_phone_number ON users(phone_number);
CREATE INDEX IF NOT EXISTS ix_vehicles_customer_id ON vehicles(customer_id);
CREATE INDEX IF NOT EXISTS ix_vehicles_vehicle_number ON vehicles(vehicle_number);
CREATE INDEX IF NOT EXISTS ix_parts_part_name ON parts(part_name);
CREATE INDEX IF NOT EXISTS ix_parts_stock_quantity ON parts(stock_quantity);
CREATE INDEX IF NOT EXISTS ix_purchase_invoices_vendor_id ON purchase_invoices(vendor_id);
CREATE INDEX IF NOT EXISTS ix_purchase_invoice_items_part_id ON purchase_invoice_items(part_id);
CREATE INDEX IF NOT EXISTS ix_sales_invoices_customer_id ON sales_invoices(customer_id);
CREATE INDEX IF NOT EXISTS ix_sales_invoices_vehicle_id ON sales_invoices(vehicle_id);
CREATE INDEX IF NOT EXISTS ix_sales_invoices_payment_status_due_date ON sales_invoices(payment_status, due_date);
CREATE INDEX IF NOT EXISTS ix_sales_invoice_items_part_id ON sales_invoice_items(part_id);
CREATE INDEX IF NOT EXISTS ix_appointments_customer_id ON appointments(customer_id);
CREATE INDEX IF NOT EXISTS ix_appointments_vehicle_id ON appointments(vehicle_id);
CREATE INDEX IF NOT EXISTS ix_part_requests_customer_id ON part_requests(customer_id);
CREATE INDEX IF NOT EXISTS ix_predictive_alerts_customer_id ON predictive_alerts(customer_id);
CREATE INDEX IF NOT EXISTS ix_predictive_alerts_vehicle_id ON predictive_alerts(vehicle_id);

INSERT INTO roles (name, description)
VALUES
    ('Admin', 'System administrator with full access'),
    ('Staff', 'Staff user handling customers, sales, and invoices'),
    ('Customer', 'Customer self-service account')
ON CONFLICT (name) DO NOTHING;

COMMIT;