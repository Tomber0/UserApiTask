using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using UserApiTask.Models;
using UserApiTask.Utils;

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

        // GET api/<UserController>?params...
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery(Name ="id")]int? id,
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
                return NotFound();
            }
            return Ok(resultUsers);
        }

        // POST api/<UserController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            
            return Ok();
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] User user)
        {

        }

        // PUT api/<UserController>/5
        [HttpPost("{id}/role")]
        public async Task<IActionResult> Put(int id, [FromBody] Role role)
        {
            var user = _context.Users.Include(u => u.Roles).FirstOrDefault(u=> u.Id.Equals(id));
            if (user == null) 
            {
                return NotFound();
            }
            user.Roles = user.Roles.Union(new List<Role>() { role }).ToList();
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            if (!(user is null))
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                return NotFound();
            }
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
