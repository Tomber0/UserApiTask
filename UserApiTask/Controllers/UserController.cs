using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UserApiTask.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UserApiTask.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private AppDbContext _context;

        public UserController(ILogger<UserController> logger, AppDbContext context)
        {
            _context = context;
            _logger = logger;
        }


        // GET api/<UserController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute(Name ="id")]int id)
        {
            var user = await _context.Users.Include(u=> u.Roles).Select(u => new User 
            {
                Id = u.Id, 
                Name = u.Name,
                Age = u.Age,
                Email = u.Email,
                Roles = u.Roles,
            }).FirstOrDefaultAsync();
            if (user == null) 
            {
                return NotFound();
            }
            return Ok(user);
        }

        // GET api/<UserController>/5
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery(Name ="id")]int id,
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
            int _userPageSize = 10;// move to a file
            IQueryable<User> users = _context.Users.Include(u=>u.Roles);
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
                return NotFound();
            }
            return Ok(resultUsers);
        }

        private static Expression<Func<User, object>> SortingParam(string? sortParam)
        {
            return sortParam.ToLower() switch
            {
                "name" => u => u.Name,
                "age" => u => u.Age,
                "email" => u => u.Email,
                "role" => u => u.Roles,
                _ => u => u.Id
            };
        }

        // POST api/<UserController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {

        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
