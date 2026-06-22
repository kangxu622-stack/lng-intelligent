CREATE TABLE IF NOT EXISTS training_manual (
    manual_id VARCHAR(32) PRIMARY KEY,
    manual_name VARCHAR(500) NOT NULL,
    file_type VARCHAR(20) NULL,
    file_path VARCHAR(1000) NULL,
    upload_user VARCHAR(100) NULL,
    upload_time DATETIME NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'uploaded'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS training_manual_chunk (
    chunk_id VARCHAR(32) PRIMARY KEY,
    manual_id VARCHAR(32) NOT NULL,
    chapter_title VARCHAR(500) NULL,
    section_no VARCHAR(50) NULL,
    content LONGTEXT NULL,
    page_no VARCHAR(50) NULL,
    system_name VARCHAR(100) NULL,
    embedding_id VARCHAR(100) NULL,
    created_time DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS training_knowledge_point (
    knowledge_id VARCHAR(32) PRIMARY KEY,
    name VARCHAR(500) NOT NULL,
    system_name VARCHAR(200) NULL,
    chapter_title VARCHAR(500) NULL,
    position VARCHAR(100) NULL,
    difficulty VARCHAR(50) NULL,
    risk_level VARCHAR(50) NULL,
    source_chunk_id VARCHAR(32) NULL,
    manual_basis VARCHAR(500) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_time DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS training_question (
    question_id VARCHAR(32) PRIMARY KEY,
    question_type VARCHAR(50) NULL,
    stem TEXT NOT NULL,
    options_json TEXT NULL,
    answer TEXT NULL,
    explanation TEXT NULL,
    knowledge_id VARCHAR(32) NULL,
    knowledge_name VARCHAR(500) NULL,
    position VARCHAR(100) NULL,
    difficulty VARCHAR(50) NULL,
    source VARCHAR(100) NULL,
    manual_basis VARCHAR(500) NULL,
    review_status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_time DATETIME NULL,
    updated_time DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS training_answer_record (
    record_id VARCHAR(32) PRIMARY KEY,
    user_id VARCHAR(100) NOT NULL,
    question_id VARCHAR(32) NULL,
    user_answer TEXT NULL,
    correct_answer TEXT NULL,
    is_correct INT NOT NULL DEFAULT 0,
    score DECIMAL(5,2) NULL,
    answer_time DATETIME NULL,
    duration INT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS training_wrong_question (
    wrong_id VARCHAR(32) PRIMARY KEY,
    user_id VARCHAR(100) NOT NULL,
    question_id VARCHAR(32) NULL,
    knowledge_id VARCHAR(32) NULL,
    wrong_answer TEXT NULL,
    correct_answer TEXT NULL,
    created_time DATETIME NULL,
    review_count INT NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS training_operation_log (
    log_id VARCHAR(32) PRIMARY KEY,
    user_id VARCHAR(100) NULL,
    action_type VARCHAR(100) NULL,
    action_detail TEXT NULL,
    created_time DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
