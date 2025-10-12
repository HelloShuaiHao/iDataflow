const express = require('express');
const router = express.Router();
const websocketController = require('../controllers/websocketController');

// WebSocket 路由
router.get('/stats', websocketController.getWebSocketStats);
router.post('/send/:companyId', websocketController.sendMessageToCompany);
router.post('/broadcast', websocketController.broadcastMessage);

module.exports = router;
