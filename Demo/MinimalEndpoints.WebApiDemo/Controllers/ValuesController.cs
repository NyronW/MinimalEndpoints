using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MinimalEndpoints.WebApiDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        /// <summary>
        /// Value API test comment
        /// </summary>
        /// <param name="foo">Foo param</param>
        /// <param name="bar">Bar param</param>
        /// <param name="baz">Baz param</param>
        /// <returns>New created item</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /todos
        ///     {        
        ///       "description": "New Task",
        ///     }
        /// </remarks>
        /// <response code="201">Returns the newly create item</response>
        /// <response code="400">Invalid data passed from client</response>
        /// <response code="401">Client is not authenticated</response>
        /// <response code="403">Client is forbiden</response>
        /// <response code="500">Internal server error occured</response>
        [HttpPost]
        public IActionResult Test(string foo, int bar, decimal baz)
        {
            return Ok(new { foo, bar, baz });
        }
    }
}
