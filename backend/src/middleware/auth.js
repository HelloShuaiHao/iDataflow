const jwt = require('jsonwebtoken');

const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key-change-in-production';

// 验证 JWT token
exports.authenticate = (req, res, next) => {
  try {
    const authHeader = req.headers.authorization;

    if (!authHeader) {
      return res.status(401).json({
        success: false,
        error: 'No authorization header',
      });
    }

    const token = authHeader.startsWith('Bearer ')
      ? authHeader.slice(7)
      : authHeader;

    const decoded = jwt.verify(token, JWT_SECRET);
    req.user = decoded;
    next();
  } catch (error) {
    if (error.name === 'TokenExpiredError') {
      return res.status(401).json({
        success: false,
        error: 'Token expired',
      });
    }

    return res.status(401).json({
      success: false,
      error: 'Invalid token',
    });
  }
};

// 验证管理员权限
exports.requireAdmin = (req, res, next) => {
  if (req.user.role !== 'admin') {
    return res.status(403).json({
      success: false,
      error: 'Admin access required',
    });
  }
  next();
};

// 验证成员权限（admin 或 member）
exports.requireMember = (req, res, next) => {
  if (req.user.role !== 'admin' && req.user.role !== 'member') {
    return res.status(403).json({
      success: false,
      error: 'Member access required',
    });
  }
  next();
};
