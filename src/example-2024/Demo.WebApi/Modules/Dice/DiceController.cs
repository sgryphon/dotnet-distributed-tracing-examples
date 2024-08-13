using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiceController : ControllerBase
    {
        private static Random Random = new Random();
        
        [HttpGet("roll")]
        public int Roll(string dice)
        {
            var result = Random.Next(1, 7);
            return result;
        }
    }
}
