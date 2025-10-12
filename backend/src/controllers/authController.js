const jwt = require('jsonwebtoken');
const User = require('../models/User');

const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key-change-in-production';
const JWT_EXPIRES_IN = process.env.JWT_EXPIRES_IN || '24h';

// 登录
exports.login = async (req, res) => {
  try {
    const { username, password } = req.body;

    if (!username || !password) {
      return res.status(400).json({
        success: false,
        error: 'Username and password are required',
      });
    }

    // 验证用户
    const user = await User.verifyPassword(username, password);

    if (!user) {
      return res.status(401).json({
        success: false,
        error: 'Invalid username or password',
      });
    }

    // 生成 JWT
    const token = jwt.sign(
      {
        id: user.id,
        username: user.username,
        role: user.role,
      },
      JWT_SECRET,
      { expiresIn: JWT_EXPIRES_IN }
    );

    res.json({
      success: true,
      data: {
        user: {
          id: user.id,
          username: user.username,
          email: user.email,
          role: user.role,
        },
        token,
      },
    });
  } catch (error) {
    console.error('Login error:', error);
    res.status(500).json({
      success: false,
      error: 'Login failed',
    });
  }
};

// 获取当前用户信息
exports.me = async (req, res) => {
  try {
    const user = await User.findById(req.user.id);

    if (!user) {
      return res.status(404).json({
        success: false,
        error: 'User not found',
      });
    }

    res.json({
      success: true,
      data: user,
    });
  } catch (error) {
    console.error('Get user error:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to get user info',
    });
  }
};

// 修改密码
exports.changePassword = async (req, res) => {
  try {
    const { oldPassword, newPassword } = req.body;

    if (!oldPassword || !newPassword) {
      return res.status(400).json({
        success: false,
        error: 'Old password and new password are required',
      });
    }

    // 验证旧密码
    const user = await User.findById(req.user.id);
    const verified = await User.verifyPassword(user.username, oldPassword);

    if (!verified) {
      return res.status(401).json({
        success: false,
        error: 'Invalid old password',
      });
    }

    // 更新密码
    await User.updatePassword(req.user.id, newPassword);

    res.json({
      success: true,
      message: 'Password changed successfully',
    });
  } catch (error) {
    console.error('Change password error:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to change password',
    });
  }
};

// 获取所有用户（仅管理员）
exports.getAllUsers = async (req, res) => {
  try {
    const users = await User.findAll();

    res.json({
      success: true,
      data: users,
    });
  } catch (error) {
    console.error('Get users error:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to get users',
    });
  }
};

// 创建用户（仅管理员）
exports.createUser = async (req, res) => {
  try {
    const { username, email, password, role } = req.body;

    if (!username || !email || !password) {
      return res.status(400).json({
        success: false,
        error: 'Username, email, and password are required',
      });
    }

    // 检查用户名是否已存在
    const existingUser = await User.findByUsername(username);
    if (existingUser) {
      return res.status(409).json({
        success: false,
        error: 'Username already exists',
      });
    }

    // 检查邮箱是否已存在
    const existingEmail = await User.findByEmail(email);
    if (existingEmail) {
      return res.status(409).json({
        success: false,
        error: 'Email already exists',
      });
    }

    // 创建用户
    const user = await User.create(username, email, password, role || 'member');

    res.status(201).json({
      success: true,
      data: user,
    });
  } catch (error) {
    console.error('Create user error:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to create user',
    });
  }
};

// 更新用户（仅管理员）
exports.updateUser = async (req, res) => {
  try {
    const { userId } = req.params;
    const { username, email, role } = req.body;

    const user = await User.update(userId, username, email, role);

    if (!user) {
      return res.status(404).json({
        success: false,
        error: 'User not found',
      });
    }

    res.json({
      success: true,
      data: user,
    });
  } catch (error) {
    console.error('Update user error:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to update user',
    });
  }
};

// 删除用户（仅管理员）
exports.deleteUser = async (req, res) => {
  try {
    const { userId } = req.params;

    // 不能删除自己
    if (parseInt(userId) === req.user.id) {
      return res.status(400).json({
        success: false,
        error: 'Cannot delete yourself',
      });
    }

    const user = await User.delete(userId);

    if (!user) {
      return res.status(404).json({
        success: false,
        error: 'User not found',
      });
    }

    res.json({
      success: true,
      message: 'User deleted successfully',
    });
  } catch (error) {
    console.error('Delete user error:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to delete user',
    });
  }
};
