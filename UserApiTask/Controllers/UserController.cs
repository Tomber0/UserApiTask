﻿using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Text;
using UserApiTask.Configurations;
using UserApiTask.Models;
using UserApiTask.Utils;

namespace UserApiTask.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IOptionsMonitor<UserConfiguration> _userConfig;
        private readonly ILogger<UserController> _logger;
        private AppDbContext _context;

        public UserController(ILogger<UserController> logger, AppDbContext context, IOptionsMonitor<UserConfiguration> userConfiguration)
        {
            _userConfig = userConfiguration;
            _context = context;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById([FromRoute(Name ="id")]int id)
        {
            _logger.LogInformation($"{Request.Method} {Request.QueryString.Value}");
            var user = await _context.Users.Include(u=> u.Roles).Where(u=> u.Id.Equals(id)).FirstOrDefaultAsync();
            if (user == null) 
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was not found (404)");
                return NotFound();
            }
            _logger.LogInformation($"Sending user with {Request.QueryString.Value}");
            return Ok(user);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUser([FromQuery(Name ="id")]int? id,
            [FromQuery(Name = "name")] string? name,
            [FromQuery(Name = "age")] int? age,
            [FromQuery(Name = "email")] string? email,
            [FromQuery(Name = "role")] string? roleName,
            [FromQuery(Name = "roleId")] int? roleId,
            [FromQuery(Name = "page")] int? page,
            [FromQuery(Name = "sortBy")] string? sortParam,
            [FromQuery(Name = "sortDir")] string? sortDirection
            )
        {
            _logger.LogInformation($"{Request.Method} {Request.QueryString.Value}");

            int _userPageSize = _userConfig.CurrentValue.PageSize;
            IQueryable<User> users = _context.Users.Include(u=>u.Roles);
            if (id.HasValue)
            {
                users = users.Where(u => u.Id.Equals(id));
            }
            if (!string.IsNullOrEmpty(name)) 
            {
                users = users.Where(u => u.Name.Equals(name));
            }
            if (age.HasValue) 
            {
                users = users.Where(u => age.Value == u.Age);
            }
            if (roleId.HasValue) 
            {
                users = users.Where(u => u.Roles.Any(r=> r.Id== roleId.Value));
            }
            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email.Equals(email));
            }
            if (!string.IsNullOrEmpty(roleName))
            {
                users = users.Where(u => u.Roles.Any(r => r.Name == roleName));
            }
            if (!string.IsNullOrEmpty(sortParam))
            {
                Expression<Func<User, object>> keyselector = SortingParam(sortParam);
                if (!string.IsNullOrEmpty(sortDirection))
                {

                    if (sortDirection.Equals("desc"))
                    {
                        users = users.OrderByDescending(keyselector);
                    }
                    else if (!sortDirection.Equals("asc")) 
                    {
                        return BadRequest();
                    }
                }
                else
                {
                    users = users.OrderBy(keyselector);
                }
            }
            if (page.HasValue)
            {
                if (page.Value < 1)
                {
                    return BadRequest();
                }
                users = users.Skip((page.Value - 1) * _userPageSize);
            }
            var resultUsers = await users.ToListAsync();
            if (!resultUsers.Any())
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User {Request.QueryString.Value} was not found (404)");
                return NotFound();
            }
            _logger.LogInformation($"Sending user with {Request.QueryString.Value}");
            return Ok(resultUsers);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            _logger.LogInformation($"{Request.Method} User");
            if (!ModelState.IsValid)
            {
                _logger.LogInformation($"[{Request.Method}] User model was invalid, getting an error message...");
                StringBuilder errorMessage = new();
                foreach (var item in ModelState)
                {
                    if (item.Value.ValidationState == ModelValidationState.Invalid)
                    {
                        errorMessage.AppendLine($"error for {item.Key}:");
                        foreach (var error in item.Value.Errors)
                        {
                            errorMessage.AppendLine($"{error.ErrorMessage}");
                        }
                    }
                }
                _logger.LogInformation($"[{Request.Method}] User model was invalid (400)");
                await LogRequest();
                return BadRequest(errorMessage.ToString());
            }
            var newUser = new User();
            if (user == null)
            {
                _logger.LogInformation($"[{Request.Method}] User model was invalid (400)");
                return BadRequest();
            }
            if (ValidateUserModel(user) && !_context.Users.Any(u => u.Email.Equals(user.Email)))
            {//для валидации данных обычно используются аттрибуты, но в ТЗ сказно проводить на уровне контроллера.
                newUser.Age = user.Age;
                newUser.Email = user.Email;
                newUser.Name = user.Name;
                newUser.Roles = newUser.Roles ?? new List<Role>();
            }
            else
            {
                _logger.LogInformation($"[{Request.Method}] User model was invalid (400)");
                await LogRequest();
                return BadRequest();
            }
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(newUser);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {

            if (!ModelState.IsValid) 
            {
                _logger.LogInformation($"[{Request.Method}] User model was invalid, getting an error message...");
                StringBuilder errorMessage =new StringBuilder();
                foreach (var item in ModelState)
                {
                    if (item.Value.ValidationState == ModelValidationState.Invalid)
                    {
                        errorMessage.AppendLine($"error for {item.Key}:");
                        foreach (var error in item.Value.Errors)
                        {
                            errorMessage.AppendLine($"{error.ErrorMessage}");
                        }
                    }
                }
                await LogRequest();
                return BadRequest(errorMessage.ToString());
            }
            var user = _context.Users.Include(u => u.Roles).FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} role was not found (404)");
                return NotFound();
            }
            if (ValidateUserModel(user) && !_context.Users.Any(u=> u.Email.Equals(user.Email)))
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] Attempting to update User id={id}");
                user.Name = updatedUser.Name;
                user.Age = updatedUser.Age;
                user.Email = updatedUser.Email;
                user.Roles = updatedUser.Roles;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was updated");
            }
            else 
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] Attempt to update User id={id} was failed! (400)");
                await LogRequest();
                return BadRequest();
            }
            _logger.LogInformation($"Created a new User (200)");
            return Ok();
        }

        [HttpPost("{id}/role")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddRoleToUserById(int id, [FromBody] Role role)
        {
            _logger.LogInformation($"{Request.Method} Add role");
            if (!ModelState.IsValid)
            {
                _logger.LogInformation($"[{Request.Method}] Role model was invalid, getting an error message...");
                StringBuilder errorMessage = new();
                foreach (var item in ModelState)
                {
                    if (item.Value.ValidationState == ModelValidationState.Invalid)
                    {
                        errorMessage.AppendLine($"error for {item.Key}:");
                        foreach (var error in item.Value.Errors)
                        {
                            errorMessage.AppendLine($"{error.ErrorMessage}");
                        }
                    }
                }
                _logger.LogInformation($"[{Request.Method}] role model was invalid (400)");
                await LogRequest();
                return BadRequest(errorMessage.ToString());
            }
            var user = _context.Users.Include(u => u.Roles).FirstOrDefault(u=> u.Id.Equals(id));
            if (user == null) 
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was not found (404)");
                return NotFound();
            }
            if (!_context.Roles.Any(r=> r.Id.Equals(role.Id) && r.Name.Equals(role.Name))) 
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] Attempt to insert an invalid role (400)");
                return BadRequest();

            }
            if (!user.Roles.Any(r=> r.Id.Equals(role.Id)))
            {
                user.Roles = user.Roles.Union(new List<Role>() { role }).ToList();
            }
            _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} role was gained:{role.Name} (200)");
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was called");
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (user is not null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was deleted (200)");
                return Ok();
            }
            else
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was not found! (400)");
                return NotFound();
            }
        }

        private static bool ValidateUserModel(User user) 
        {
            return !string.IsNullOrEmpty(user.Name) &&
                    user.Age > 0 &&
                    !string.IsNullOrEmpty(user.Email);
        }

        async Task LogRequest()
        {
            StringBuilder logMessage = new();
            logMessage.Append($"method: [{Request.Method.ToUpper()}]");
            logMessage.Append($"path: {Request.Path}");
            Request.EnableBuffering();
            var requestReader = new StreamReader(Request.Body);
            var content = await requestReader.ReadToEndAsync();
            logMessage.AppendLine($"body: {content}");
            _logger.LogInformation($"{logMessage}");
        }

        private static Expression<Func<User, object>> SortingParam(string? sortParam)
        {
            return sortParam.ToLower() switch
            {
                "name" => u => u.Name,
                "age" => u => u.Age,
                "email" => u => u.Email,
                "rolename" => u => u.Roles.Select(r => r.Name),
                "roleid" => u => u.Roles.Select(r => r.Id),
                _ => u => u.Id
            };
        }
    }
}
