-- LNG接收站智能启停系统 数据库初始化脚本

-- 角色表
CREATE TABLE IF NOT EXISTS sys_role (
    role_id INT PRIMARY KEY AUTO_INCREMENT,
    role_code VARCHAR(50) NOT NULL,
    role_name VARCHAR(100) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 用户表
CREATE TABLE IF NOT EXISTS sys_user (
    user_id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NULL,
    email VARCHAR(255) NULL,
    phone VARCHAR(50) NULL,
    department VARCHAR(100) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- LLM 会话表
CREATE TABLE IF NOT EXISTS llm_conversation (
    conversation_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    conversation_code VARCHAR(100) NOT NULL,
    user_id INT NOT NULL,
    biz_type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    summary TEXT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    last_message_at DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- LLM 消息表
CREATE TABLE IF NOT EXISTS llm_message (
    message_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    conversation_id BIGINT NOT NULL,
    role VARCHAR(20) NOT NULL,
    content_type VARCHAR(20) NOT NULL DEFAULT 'text',
    content LONGTEXT NOT NULL,
    model_name VARCHAR(100) NULL,
    prompt_tokens INT NULL,
    completion_tokens INT NULL,
    total_tokens INT NULL,
    response_ms INT NULL,
    sequence_no INT NOT NULL,
    parent_message_id BIGINT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- LLM 附件表
CREATE TABLE IF NOT EXISTS llm_attachment (
    attachment_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    conversation_id BIGINT NOT NULL,
    message_id BIGINT NULL,
    attachment_type VARCHAR(50) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_ext VARCHAR(20) NULL,
    mime_type VARCHAR(100) NULL,
    file_size BIGINT NULL,
    storage_path VARCHAR(500) NOT NULL,
    preview_url VARCHAR(500) NULL,
    ocr_text TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 插入默认角色
INSERT INTO sys_role (role_code, role_name, is_active) VALUES
('ADMIN', '管理员', 1),
('OPERATOR', '操作员', 1),
('VISITOR', '访客', 1);

-- 插入默认管理员用户（密码: admin123）
INSERT INTO sys_user (username, password_hash, role_id, is_active)
SELECT 'admin', 'admin123', r.role_id, 1
FROM sys_role r
WHERE r.role_code = 'ADMIN';
