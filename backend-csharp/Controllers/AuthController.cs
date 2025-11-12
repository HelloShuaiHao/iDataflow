using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using iDataflow.Backend.Services;
using iDataflow.Backend.DTOs;

namespace iDataflow.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserService userService, JwtService jwtService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Error = "Username and password are required"
                    });
                }

                var user = await _userService.VerifyPasswordAsync(request.Username, request.Password);

                if (user == null)
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Error = "Invalid username or password"
                    });
                }

                var token = _jwtService.GenerateToken(user);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new LoginResponse
                    {
                        User = new UserDto
                        {
                            Id = user.Id,
                            Username = user.Username,
                            Email = user.Email,
                            Role = user.Role
                        },
                        Token = token
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user {Username}", request.Username);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Login failed"
                });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
                var user = await _userService.FindByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Error = "User not found"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get user error");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to get user info"
                });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Error = "Old password and new password are required"
                    });
                }

                var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
                var username = User.FindFirst("username")?.Value ?? "";

                // Verify old password
                var user = await _userService.VerifyPasswordAsync(username, request.OldPassword);
                if (user == null)
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Error = "Invalid old password"
                    });
                }

                // Update password
                await _userService.UpdatePasswordAsync(userId, request.NewPassword);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new { Message = "Password changed successfully" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password error");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to change password"
                });
            }
        }

        [HttpGet("users")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                var userDtos = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role
                }).ToList();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = userDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get users error");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to get users"
                });
            }
        }

        [HttpPost("users")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Error = "Username, email, and password are required"
                    });
                }

                // Check if user exists
                if (await _userService.UserExistsAsync(request.Username, request.Email))
                {
                    return Conflict(new ApiResponse
                    {
                        Success = false,
                        Error = "Username or email already exists"
                    });
                }

                var user = await _userService.CreateUserAsync(request.Username, request.Email, request.Password, request.Role ?? "member");

                return CreatedAtAction(nameof(GetAllUsers), new ApiResponse
                {
                    Success = true,
                    Data = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create user error");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to create user"
                });
            }
        }

        [HttpPut("users/{userId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _userService.UpdateUserAsync(userId, request.Username, request.Email, request.Role);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Error = "User not found"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update user error");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to update user"
                });
            }
        }

        [HttpDelete("users/{userId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");

                if (userId == currentUserId)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Error = "Cannot delete yourself"
                    });
                }

                var deleted = await _userService.DeleteUserAsync(userId);

                if (!deleted)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Error = "User not found"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new { Message = "User deleted successfully" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete user error");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to delete user"
                });
            }
        }
    }
}