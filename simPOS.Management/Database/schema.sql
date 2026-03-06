-- ============================================
-- simPOS Database Schema
-- File: schema.sql
-- ============================================

PRAGMA foreign_keys = ON;

-- ============================================
-- TABEL: categories
-- ============================================
CREATE TABLE IF NOT EXISTS categories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT    NOT NULL UNIQUE,
    description TEXT,
    created_at  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- ============================================
-- TABEL: suppliers
-- ============================================
CREATE TABLE IF NOT EXISTS suppliers (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    name           TEXT NOT NULL,
    contact_person TEXT,
    phone          TEXT,
    email          TEXT,
    address        TEXT,
    is_active      INTEGER NOT NULL DEFAULT 1,
    created_at     TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- ============================================
-- TABEL: products
-- ============================================
CREATE TABLE IF NOT EXISTS products (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id INTEGER REFERENCES categories(id) ON DELETE SET NULL,
    supplier_id INTEGER REFERENCES suppliers(id)  ON DELETE SET NULL,
    code        TEXT    NOT NULL UNIQUE,
    name        TEXT    NOT NULL,
    description TEXT,
    unit        TEXT    NOT NULL DEFAULT 'pcs',
    buy_price   REAL    NOT NULL DEFAULT 0,
    sell_price  REAL    NOT NULL DEFAULT 0,
    stock       INTEGER NOT NULL DEFAULT 0,
    min_stock   INTEGER NOT NULL DEFAULT 0,
    is_active   INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    updated_at  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- ============================================
-- TABEL: goods_receipts (Header Penerimaan Barang)
-- Satu dokumen = satu pengiriman dari supplier.
-- Detail barangnya ada di stock_movements.
-- ============================================
CREATE TABLE IF NOT EXISTS goods_receipts (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    receipt_no  TEXT    NOT NULL UNIQUE,
    supplier_id INTEGER REFERENCES suppliers(id) ON DELETE SET NULL,
    notes       TEXT,
    received_at TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    created_at  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- ============================================
-- TABEL: stock_movements (Detail / Line Item)
-- Dipakai untuk: Penerimaan Barang (IN), Penjualan (OUT), Stock Opname (ADJUSTMENT)
-- receipt_id hanya terisi untuk type = 'IN'
-- ============================================
CREATE TABLE IF NOT EXISTS stock_movements (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id   INTEGER NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    receipt_id   INTEGER REFERENCES goods_receipts(id) ON DELETE SET NULL,
    type         TEXT    NOT NULL CHECK(type IN ('IN', 'OUT', 'ADJUSTMENT')),
    quantity     INTEGER NOT NULL,
    buy_price    REAL    NOT NULL DEFAULT 0,
    notes        TEXT,
    reference_no TEXT,
    created_at   TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- ============================================
-- DATA AWAL (Seed)
-- ============================================
INSERT OR IGNORE INTO categories (name, description) VALUES
    ('Makanan',   'Produk makanan dan camilan'),
    ('Minuman',   'Produk minuman'),
    ('Elektronik','Produk elektronik & aksesoris'),
    ('Lainnya',   'Kategori umum');

-- ============================================
-- TABEL: stock_opnames (Header Stock Opname)
-- Satu sesi opname bisa mencakup semua atau sebagian barang.
-- Detail penyesuaian ada di stock_movements (type=ADJUSTMENT).
-- ============================================
CREATE TABLE IF NOT EXISTS stock_opnames (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    opname_no    TEXT    NOT NULL UNIQUE,
    notes        TEXT,
    status       TEXT    NOT NULL DEFAULT 'DRAFT' CHECK(status IN ('DRAFT','CONFIRMED')),
    confirmed_at TEXT,
    created_at   TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- opname_id di stock_movements — terisi untuk type = ADJUSTMENT dari opname
-- (ditambahkan via MigrateIfNeeded agar tidak breaking database lama)

-- ============================================
-- TABEL: transactions (Header Transaksi POS)
-- ============================================
CREATE TABLE IF NOT EXISTS transactions (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    invoice_no     TEXT    NOT NULL UNIQUE,
    total_amount   REAL    NOT NULL DEFAULT 0,
    paid_amount    REAL    NOT NULL DEFAULT 0,
    change_amount  REAL    NOT NULL DEFAULT 0,
    payment_method TEXT    NOT NULL DEFAULT 'CASH',
    notes          TEXT,
    created_at     TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

-- ============================================
-- TABEL: transaction_items (Detail Transaksi POS)
-- ============================================
CREATE TABLE IF NOT EXISTS transaction_items (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    transaction_id INTEGER NOT NULL REFERENCES transactions(id) ON DELETE CASCADE,
    product_id     INTEGER NOT NULL REFERENCES products(id)     ON DELETE RESTRICT,
    product_code   TEXT    NOT NULL,
    product_name   TEXT    NOT NULL,
    unit           TEXT    NOT NULL,
    quantity       INTEGER NOT NULL,
    sell_price     REAL    NOT NULL,
    subtotal       REAL    NOT NULL
);