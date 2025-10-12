const websocketService = require('../services/websocketService');

// 获取 WebSocket 连接统计
exports.getWebSocketStats = (req, res) => {
  try {
    const stats = websocketService.getStats();
    res.json({
      success: true,
      data: stats,
    });
  } catch (error) {
    console.error('Error getting WebSocket stats:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to get WebSocket stats',
    });
  }
};

// 向特定公司发送消息
exports.sendMessageToCompany = (req, res) => {
  try {
    const { companyId } = req.params;
    const { message } = req.body;

    const sent = websocketService.sendToCompany(companyId, message);

    if (sent) {
      res.json({
        success: true,
        message: 'Message sent successfully',
      });
    } else {
      res.status(404).json({
        success: false,
        error: 'Company WebSocket connection not found',
      });
    }
  } catch (error) {
    console.error('Error sending message:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to send message',
    });
  }
};

// 广播消息给所有客户端
exports.broadcastMessage = (req, res) => {
  try {
    const { message } = req.body;
    websocketService.broadcast(message);

    res.json({
      success: true,
      message: 'Message broadcasted successfully',
    });
  } catch (error) {
    console.error('Error broadcasting message:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to broadcast message',
    });
  }
};
