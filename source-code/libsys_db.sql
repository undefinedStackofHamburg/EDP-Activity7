-- ============================================================
--  LibSys — MySQL Database Schema
--  File   : libsys_db.sql
--  Run in : MySQL Workbench or phpMyAdmin (XAMPP)
--
--  HOW TO USE:
--  1. Open MySQL Workbench and connect to your XAMPP MySQL
--     (host: localhost, port: 3306, user: root, pass: empty)
--  2. Open this file → click the lightning bolt (Execute All)
--  3. Done. The database "libsys_db" will be created with
--     all tables and seed data already inserted.
-- ============================================================

-- Drop and recreate the database so this script is idempotent
-- (safe to run multiple times — it wipes and rebuilds)


DROP DATABASE IF EXISTS libsys_db; -- use this line for resetting the whole db 
CREATE DATABASE libsys_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE libsys_db;

-- ─────────────────────────────────────────────────────────────
--  TABLE: categories
--  Stores book genre categories (Fantasy, Technology, etc.)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE categories (
  id          INT           NOT NULL AUTO_INCREMENT,
  name        VARCHAR(100)  NOT NULL,
  description VARCHAR(255)  NOT NULL DEFAULT '',
  PRIMARY KEY (id)
);

-- ─────────────────────────────────────────────────────────────
--  TABLE: authors
--  Stores author information linked to books
-- ─────────────────────────────────────────────────────────────
CREATE TABLE authors (
  id          INT           NOT NULL AUTO_INCREMENT,
  first_name  VARCHAR(100)  NOT NULL,
  last_name   VARCHAR(100)  NOT NULL,
  nationality VARCHAR(100)  NOT NULL DEFAULT '',
  birth_year  INT           NOT NULL DEFAULT 0,
  PRIMARY KEY (id)
);

-- ─────────────────────────────────────────────────────────────
--  TABLE: books
--  Stores the book catalog
--  author_id → references authors(id)
--  cat_id    → references categories(id)
--  total     → total physical copies in the library
--  available → copies not currently on loan
-- ─────────────────────────────────────────────────────────────
CREATE TABLE books (
  id          INT           NOT NULL AUTO_INCREMENT,
  title       VARCHAR(255)  NOT NULL,
  author_id   INT           NOT NULL,
  cat_id      INT           NOT NULL,
  isbn        VARCHAR(50)   NOT NULL DEFAULT '',
  year_pub    INT           NOT NULL DEFAULT 0,
  total       INT           NOT NULL DEFAULT 1,
  available   INT           NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  CONSTRAINT fk_book_author   FOREIGN KEY (author_id) REFERENCES authors(id),
  CONSTRAINT fk_book_category FOREIGN KEY (cat_id)    REFERENCES categories(id)
);

-- ─────────────────────────────────────────────────────────────
--  TABLE: members
--  Library card holders
--  status: 'Active' | 'Suspended' | 'Expired'
-- ─────────────────────────────────────────────────────────────
CREATE TABLE members (
  id              INT           NOT NULL AUTO_INCREMENT,
  first_name      VARCHAR(100)  NOT NULL,
  last_name       VARCHAR(100)  NOT NULL,
  phone           VARCHAR(20)   NOT NULL DEFAULT '',
  membership_date DATE          NOT NULL,
  status          VARCHAR(20)   NOT NULL DEFAULT 'Active',
  PRIMARY KEY (id)
);

-- ─────────────────────────────────────────────────────────────
--  TABLE: loans
--  Tracks every book borrow/return transaction
--  status: 'Active' | 'Returned' | 'Overdue'
--  fine_amount: auto-calculated as ₱5.00 per day overdue
-- ─────────────────────────────────────────────────────────────
CREATE TABLE loans (
  id          INT           NOT NULL AUTO_INCREMENT,
  book_id     INT           NOT NULL,
  member_id   INT           NOT NULL,
  loan_date   DATE          NOT NULL,
  due_date    DATE          NOT NULL,
  return_date DATE          NULL,
  fine_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  status      VARCHAR(20)   NOT NULL DEFAULT 'Active',
  PRIMARY KEY (id),
  CONSTRAINT fk_loan_book   FOREIGN KEY (book_id)   REFERENCES books(id),
  CONSTRAINT fk_loan_member FOREIGN KEY (member_id) REFERENCES members(id)
);

-- ─────────────────────────────────────────────────────────────
--  TABLE: users
--  System accounts for logging into LibSys.
--  role:   'Admin' | 'Staff'
--  status: 'Active' | 'Inactive'
--
--  password_hash stores a SHA2-256 hash of the password.
--  In a real production app you would use bcrypt,
--  but SHA2 is fine for a demo/activity project.
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
  id            INT           NOT NULL AUTO_INCREMENT,
  username      VARCHAR(50)   NOT NULL UNIQUE,
  full_name     VARCHAR(150)  NOT NULL,
  email         VARCHAR(150)  NOT NULL DEFAULT '',
  role          VARCHAR(20)   NOT NULL DEFAULT 'Staff',
  password_hash VARCHAR(255)  NOT NULL,
  status        VARCHAR(20)   NOT NULL DEFAULT 'Active',
  created_at    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id)
);


-- ─────────────────────────────────────────────────────────────
--  SEED DATA — mirrors the JS DB object exactly
-- ─────────────────────────────────────────────────────────────

INSERT INTO categories (id, name, description) VALUES
  (1,  'Fantasy',     'Magics, mythical creatures...'),
  (2,  'Technology',  'Computing, engineering, IT'),
  (3,  'Mystery',     'Detectives, intriguing stories'),
  (4,  'Sci-Fi',      'Tech, gadgets, space'),
  (5,  'Fiction',     'Imaginative stories'),
  (6,  'Philosophy',  'Philosophical thoughts'),
  (7,  'Horror',      'Spooky, scary stories'),
  (8,  'Biography',   'Life stories of real people'),
  (9,  'History',     'Historical events and figures'),
  (10, 'Non-Fiction', 'Real-world content');

INSERT INTO authors (id, first_name, last_name, nationality, birth_year) VALUES
  (1,  'Geoff Larenz', 'Peñafiel',   'Filipino', 2005),
  (2,  'Keiron',       'Mirandilla', 'Filipino', 1994),
  (3,  'Joemar Ammiel',"Bru'as",     'British',  2001),
  (4,  'Xian Lei',     'Sierra',     'Chinese',  2003),
  (5,  'Yuuta',        'Nagisa',     'Japanese', 2003),
  (6,  'Ryan',         'Yung-bi',    'Korean',   2002),
  (7,  'Frederich',    'Baldano',    'German',   1999),
  (8,  'Rein',         'Capitullo',  'German',   2005),
  (9,  'Alejandrino',  'Beato',      'Israeli',  2004),
  (10, 'Alaijro',      'Abion',      'Israeli',  2004);

INSERT INTO books (id, title, author_id, cat_id, isbn, year_pub, total, available) VALUES
  (1,  'IT 101: How 2 Code?',                          1,  2, '676-42067', 2010, 5,  3),
  (2,  'IT 101: Exploring the Web',                    1,  2, '682-41092', 2009, 3,  1),
  (3,  'IT 101: A deep dive into Cybersecurity',       2,  2, '841-23312', 2010, 5,  1),
  (4,  'IT 101: Meet Linux',                           2,  2, '412-86432', 2012, 6,  4),
  (5,  'Hell Week',                                    3,  7, '892-32614', 2026, 1,  0),
  (6,  'Meet Arvie G: The man behind the system',      3,  8, '142-3145',  2025, 5,  1),
  (7,  'The Human Value of Survival',                  4,  6, '884-19472', 2007, 10, 3),
  (8,  "Japan's Advanced Technology Architecture Today",6, 4, '121-32123', 2020, 4,  2),
  (9,  'IT 404: Who Stole My Cookies?',                6,  3, '121-32823', 2016, 9,  8),
  (10, 'The Great Innovation 2012th Edition',          7,  10,'741-09721', 2012, 8,  3),
  (11, 'The Great Heroes of the past',                 8,  9, '312-54212', 2008, 4,  2),
  (12, 'IT 101: The Internet that surrounds us',       9,  2, '642-87931', 2011, 7,  5),
  (13, 'How to get a "Girlfriend"',                    10, 5, '67-42067',  2010, 5,  3),
  (14, "Airo's Adventure: The Reign of Allen",         10, 1, '632-67676', 2019, 10, 4);

INSERT INTO members (id, first_name, last_name, phone, membership_date, status) VALUES
  (1,  'Juan',     'Montellejos', '0932123', '2023-03-15', 'Active'),
  (2,  'Pedro',    'Caballo',     '0932143', '2023-05-18', 'Active'),
  (3,  'John',     'Doe',         '0932111', '2023-05-18', 'Active'),
  (4,  'Carlo',    'Dalipay',     '0932222', '2023-06-05', 'Suspended'),
  (5,  'Jhon Del', 'Castillo',    '0932333', '2023-07-22', 'Active'),
  (6,  'Samuel',   'Aquino',      '0932555', '2023-08-07', 'Active'),
  (7,  'Ashley',   'Santos',      '0932444', '2023-08-21', 'Active'),
  (8,  'Micaiah',  'Santos',      '0932442', '2023-08-25', 'Expired'),
  (9,  'Ana',      'Garcia',      '0932445', '2023-09-06', 'Active'),
  (10, 'Miguel',   'Del Pilar',   '0932543', '2024-01-10', 'Active');

INSERT INTO loans (id, book_id, member_id, loan_date, due_date, return_date, fine_amount, status) VALUES
  (1,  1,  1,  '2024-01-05', '2024-01-19', '2024-01-18', 0.00,  'Returned'),
  (2,  2,  2,  '2024-01-10', '2024-01-24', NULL,         0.00,  'Active'),
  (3,  3,  3,  '2024-01-12', '2024-01-26', '2024-02-01', 30.00, 'Returned'),
  (4,  4,  3,  '2024-02-01', '2024-02-15', NULL,         0.00,  'Active'),
  (5,  6,  4,  '2024-02-03', '2024-01-17', NULL,         0.00,  'Overdue'),
  (6,  6,  6,  '2024-02-05', '2024-01-19', '2024-02-19', 0.00,  'Returned'),
  (7,  8,  3,  '2024-02-10', '2024-02-24', NULL,         0.00,  'Active'),
  (8,  3,  5,  '2024-02-15', '2024-03-01', '2024-03-10', 45.00, 'Returned'),
  (9,  1,  9,  '2024-03-01', '2024-03-15', NULL,         0.00,  'Active'),
  (10, 10, 10, '2024-03-05', '2024-03-19', NULL,         0.00,  'Active');

INSERT INTO users (id, username, full_name, email, role, password_hash, status) VALUES
  (1, 'admin', 'Administrator',  'admin@libsys.local',  'Admin', SHA2('password', 256), 'Active'),
  (2, 'staff1','Juan Librarian', 'juan@libsys.local',   'Staff', SHA2('password', 256), 'Active'),
  (3, 'staff2','Maria Reyes',    'maria@libsys.local',  'Staff', SHA2('password', 256), 'Inactive');


CREATE TABLE IF NOT EXISTS users (
  id            INT           NOT NULL AUTO_INCREMENT,
  username      VARCHAR(50)   NOT NULL UNIQUE,
  full_name     VARCHAR(150)  NOT NULL,
  email         VARCHAR(150)  NOT NULL DEFAULT '',
  role          VARCHAR(20)   NOT NULL DEFAULT 'Staff',
  password_hash VARCHAR(255)  NOT NULL,
  status        VARCHAR(20)   NOT NULL DEFAULT 'Active',
  created_at    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id)
);


-- ─────────────────────────────────────────────────────────────
--  USEFUL VIEWS (optional but handy in Workbench)
-- ─────────────────────────────────────────────────────────────

-- Full loan details with member and book names
CREATE OR REPLACE VIEW vw_loan_details AS
SELECT
  l.id,
  CONCAT(m.first_name, ' ', m.last_name) AS member_name,
  b.title                                AS book_title,
  l.loan_date,
  l.due_date,
  l.return_date,
  l.fine_amount,
  l.status
FROM loans l
JOIN members m ON m.id = l.member_id
JOIN books   b ON b.id = l.book_id;

-- Books with author and category names
CREATE OR REPLACE VIEW vw_book_details AS
SELECT
  b.id,
  b.title,
  CONCAT(a.first_name, ' ', a.last_name) AS author_name,
  c.name                                 AS category_name,
  b.isbn,
  b.year_pub,
  b.total,
  b.available,
  (b.total - b.available)                AS on_loan
FROM books      b
JOIN authors    a ON a.id = b.author_id
JOIN categories c ON c.id = b.cat_id;
