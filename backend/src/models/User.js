const { query } = require('../config/database');
const bcrypt = require('bcrypt');

class User {
  // 获取所有用户
  static async findAll() {
    const result = await query(
      `SELECT id, username, email, role, created_at, updated_at
       FROM users ORDER BY created_at DESC`
    );
    return result.rows;
  }

  // 根据 ID 获取用户
  static async findById(id) {
    const result = await query(
      `SELECT id, username, email, role, created_at, updated_at
       FROM users WHERE id = $1`,
      [id]
    );
    return result.rows[0];
  }

  // 根据 username 获取用户（包含密码）
  static async findByUsername(username) {
    const result = await query(
      `SELECT * FROM users WHERE username = $1`,
      [username]
    );
    return result.rows[0];
  }

  // 根据 email 获取用户
  static async findByEmail(email) {
    const result = await query(
      `SELECT id, username, email, role, created_at, updated_at
       FROM users WHERE email = $1`,
      [email]
    );
    return result.rows[0];
  }

  // 创建用户
  static async create(username, email, password, role = 'member') {
    // 哈希密码
    const saltRounds = 10;
    const passwordHash = await bcrypt.hash(password, saltRounds);

    const result = await query(
      `INSERT INTO users (username, email, password_hash, role)
       VALUES ($1, $2, $3, $4)
       RETURNING id, username, email, role, created_at, updated_at`,
      [username, email, passwordHash, role]
    );
    return result.rows[0];
  }

  // 更新用户
  static async update(id, username, email, role) {
    const result = await query(
      `UPDATE users
       SET username = $1, email = $2, role = $3
       WHERE id = $4
       RETURNING id, username, email, role, created_at, updated_at`,
      [username, email, role, id]
    );
    return result.rows[0];
  }

  // 更新密码
  static async updatePassword(id, newPassword) {
    const saltRounds = 10;
    const passwordHash = await bcrypt.hash(newPassword, saltRounds);

    const result = await query(
      `UPDATE users
       SET password_hash = $1
       WHERE id = $2
       RETURNING id, username, email, role`,
      [passwordHash, id]
    );
    return result.rows[0];
  }

  // 删除用户
  static async delete(id) {
    const result = await query(
      'DELETE FROM users WHERE id = $1 RETURNING id, username, email',
      [id]
    );
    return result.rows[0];
  }

  // 验证密码
  static async verifyPassword(username, password) {
    const user = await this.findByUsername(username);
    if (!user) {
      return null;
    }

    const isValid = await bcrypt.compare(password, user.password_hash);
    if (!isValid) {
      return null;
    }

    // 返回用户信息（不包含密码）
    const { password_hash, ...userWithoutPassword } = user;
    return userWithoutPassword;
  }
}

module.exports = User;
