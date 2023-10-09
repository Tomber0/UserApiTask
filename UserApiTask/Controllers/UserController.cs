using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UserApiTask.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // GET: api/<UserController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<UserController>/5
        [HttpGet("{id}")]
        public string Get([FromQuery(Name ="id")]int id,
            [FromQuery(Name = "name")] string name,
            [FromQuery(Name = "age")] int age,
            [FromQuery(Name = "email")] string email,
            [FromQuery(Name = "role")] string roleName,
            [FromQuery(Name = "page")] int page,
            [FromQuery(Name = "sortBy")] string sortParam,
            [FromQuery(Name = "sortDir")] string sortDirection
            )
        {
            return "value";
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
