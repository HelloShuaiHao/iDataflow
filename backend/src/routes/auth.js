const express = require('express');
const router = express.Router();
const authController = require('../controllers/authController');
const { authenticate, requireAdmin } = require('../middleware/auth');

// 公开路由
router.post('/login', authController.login);

// 需要认证的路由
router.get('/me', authenticate, authController.me);
router.post('/change-password', authenticate, authController.changePassword);

// 仅管理员路由
router.get('/users', authenticate, requireAdmin, authController.getAllUsers);
router.post('/users', authenticate, requireAdmin, authController.createUser);
router.put('/users/:userId', authenticate, requireAdmin, authController.updateUser);
router.delete('/users/:userId', authenticate, requireAdmin, authController.deleteUser);

module.exports = router;
