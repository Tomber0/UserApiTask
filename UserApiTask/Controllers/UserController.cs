using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
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
        /// <summary>
        /// Get user by Id
        /// </summary>
        /// <remarks>
        /// Request example:
        /// 
        /// GET User/1
        /// </remarks>
        /// <param name="id">User Id(num)</param>
        /// <returns></returns>
        /// <response code="200">Return a User</response>
        /// <response code="404">User with Id was not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById([FromRoute(Name ="id")]int id)
        {
            _logger.LogInformation($"{Request.Method} {Request.QueryString.Value}");
            var user = await _context.Users.Include(u=> u.Roles).Where(u=> u.Id.Equals(id)).FirstOrDefaultAsync();
            if (user == null) 
            {
                string errorMsg = $"[{Request.Method.ToUpper()}] User with id={id} was not found (404)";
                _logger.LogInformation(errorMsg);
                return NotFound(errorMsg);
            }
            _logger.LogInformation($"Sending user with {Request.QueryString.Value}");
            return Ok(user);
        }

        /// <summary>
        /// Get user by it's values
        /// </summary>
        /// <remarks>
        /// Request example:
        /// 
        /// GET User?id=1&amp;name=name
        /// </remarks>
        /// <param name="id">User Id(num)</param>
        /// <param name="name">User Name(string)</param>
        /// <param name="age">User Age(num)</param>
        /// <param name="email">User Email(string)</param>
        /// <param name="roleName">User's Role Name(string)</param>
        /// <param name="roleId">User's Role Id(num)</param>
        /// <param name="page">Page(num)</param>
        /// <param name="sortParam">Name of field to order User objects('id','name','email','age','rolename','roleid')</param>
        /// <param name="sortDirection">Order direction ('asc','desc')</param>
        /// <returns></returns>
        ///<response code="200">Return a User</response>
        /// <response code="404">User was not found</response>
        /// <response code="400">Wrong passed parameters</response>
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
                    string errorMsg = $"[{Request.Method.ToUpper()}] Page cant be negative or 0 (400)";
                    _logger.LogInformation($"Sending user with {Request.QueryString.Value}");
                    return BadRequest(errorMsg);
                }
                users = users.Skip((page.Value - 1) * _userPageSize);
            }
            var resultUsers = await users.ToListAsync();
            if (!resultUsers.Any())
            {
                string errorMsg = $"[{Request.Method.ToUpper()}] User {Request.QueryString.Value} was not found (404)";
                _logger.LogInformation(errorMsg);
                return NotFound(errorMsg);
            }
            _logger.LogInformation($"Sending user with {Request.QueryString.Value}");
            return Ok(resultUsers);
        }

        /// <summary>
        /// Creates a new User
        /// </summary>
        /// <remarks>
        /// Request example:
        /// POST User
        /// {
        ///  
        ///  "name": "string",
        ///  "age": 0,
        ///  "email": "string",
        ///  "roles": [
        ///    {
        ///      "id": 0,
        ///      "name": "string"
        ///    }
        ///  ]
        ///}
        /// 
        /// </remarks>
        /// <param name="user">User model</param>
        /// <returns></returns>
        ///<response code="200">Return a new User</response>
        /// <response code="404">User was not found</response>
        /// <response code="400">Provided body was not correct</response>
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
                var rolesId = user?.Roles.Select(r => r.Id).ToList();
                var newRoles = _context.Roles.Where(r => rolesId.Any(u => u.Equals(r.Id))).ToList();
                newUser.Roles = newRoles;
                var createdUser = _context.Users.Add(newUser);
                _logger.LogInformation($"[{Request.Method}] User {newUser.Id} was inserted (200)");
            }
            else
            {
                string errorMsg = $"[{Request.Method}] User model was invalid (400)";
                _logger.LogInformation(errorMsg);
                await LogRequest();
                return BadRequest(errorMsg);
            }
            await _context.SaveChangesAsync();
            return Ok(newUser);
        }

        /// <summary>
        /// Updates a User
        /// </summary>
        /// <remarks>
        /// Request example:
        /// PUT User/1
        /// {
        ///  
        ///  "name": "string",
        ///  "age": 0,
        ///  "email": "string",
        ///  "roles": [
        ///    {
        ///      "id": 0,
        ///      "name": "string"
        ///    }
        ///  ]
        ///}
        /// 
        /// </remarks>
        /// <param name="id">User's id</param>
        /// <param name="updatedUser">User model</param>
        /// <returns></returns>
        ///<response code="200">Return an updated User</response>
        /// <response code="404">User was not found</response>
        /// <response code="400">Provided body was not correct</response>

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
                string errorMsg = $"[{Request.Method.ToUpper()}] User with id={id} role was not found (404)";
                _logger.LogInformation(errorMsg);
                return NotFound(errorMsg);
            }
            if (ValidateUserModel(user) && !_context.Users.Any(u=> u.Email.Equals(updatedUser.Email)))
            {
                _logger.LogInformation($"[{Request.Method.ToUpper()}] Attempting to update User id={id}");
                user.Name = updatedUser.Name;
                user.Age = updatedUser.Age;
                user.Email = updatedUser.Email;
                var rolesId = updatedUser?.Roles.Select(r=>r.Id).ToList();
                var newRoles = _context.Roles.Where(r => rolesId.Any(u=>u.Equals(r.Id))).ToList();
                user.Roles = newRoles;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} was updated");
            }
            else 
            {
                string errorMsg = $"[{Request.Method.ToUpper()}] Attempt to update User id={id} was failed! (400)";
                _logger.LogInformation(errorMsg);
                await LogRequest();
                return BadRequest(errorMsg);
            }
            _logger.LogInformation($"Created a new User (200)");
            return Ok(user);
        }

        /// <summary>
        /// Creates a new User
        /// </summary>
        /// <remarks>
        /// Request example:
        /// POST User/1/role
        /// 
        /// {
        ///  "id": 0,
        ///  "name": "string"
        ///}
        /// 
        /// </remarks>
        /// <param name="role">Role model</param>
        /// <param name="id">User's id(num)</param>
        /// <returns></returns>
        ///<response code="200">Return an updated User with new role</response>
        /// <response code="404">User was not found</response>
        /// <response code="400">Provided body was not correct</response>
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
                string errorMsg = $"[{Request.Method.ToUpper()}] User with id={id} was not found (404)";
                _logger.LogInformation(errorMsg);
                return NotFound(errorMsg);
            }
            if (!_context.Roles.Any(r=> r.Id.Equals(role.Id) && r.Name.Equals(role.Name))) 
            {
                string errorMsg = $"[{Request.Method.ToUpper()}] Attempt to insert an invalid role (400)";
                _logger.LogInformation(errorMsg);
                return BadRequest(errorMsg);

            }
            if (!user.Roles.Any(r=> r.Id.Equals(role.Id)))
            {
                user.Roles = user.Roles.Union(new List<Role>() { role }).ToList();
            }
            _logger.LogInformation($"[{Request.Method.ToUpper()}] User with id={id} role was gained:{role.Name} (200)");
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        /// <summary>
        /// Deletes user by Id
        /// </summary>
        /// <remarks>
        /// Request example:
        /// 
        /// DELETE User/1
        /// </remarks>
        /// <param name="id">User's Id(num)</param>
        /// <returns></returns>
        /// <response code="200">User was deleted</response>
        /// <response code="404">User with Id was not found</response>
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
                "rolename" => u => u.Roles.OrderBy(l=>l.Name).FirstOrDefault().Name,
                "roleid" => u => u.Roles.OrderBy(l => l.Id).FirstOrDefault().Id,
                _ => u => u.Id
            };
        }
    }
}
