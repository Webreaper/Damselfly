using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers
{
    [Authorize(Policy = PolicyDefinitions.s_IsAdmin)]
    [ApiController]
    [Route("/api/[controller]")]
    public class ProductController(ProductService productService) : Controller
    {
        private readonly ProductService _productService = productService;

        [HttpPost]
        [Route("create")]
        [ProducesResponseType(typeof(ProductModel), 200)]
        public async Task<IActionResult> CreatePhotoShoot(ProductModel productModel)
        {
            var created = await _productService.CreateProduct(productModel);
            return Ok(created);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeletePhotoShoot(string id)
        {
            if( !Guid.TryParse(id, out var productId) )
                return BadRequest("id is required");
            var success = await _productService.DeleteProduct(productId);
            if( !success )
            {
                return BadRequest();
            }

            return Ok(new BooleanResultModel { Result = true });
        }

        [HttpPost]
        [Route("update")]
        [ProducesResponseType(typeof(ProductModel), 200)]
        public async Task<IActionResult> UpdatePhotoShoot(ProductModel productModel)
        {
            var result = await _productService.UpdateProduct(productModel);
            return Ok(result);
        }

        [HttpGet]
        [Route("list")]
        [ProducesResponseType(typeof(List<ProductModel>), 200)]
        public async Task<IActionResult> GetProducts()
        {
            var result = await _productService.GetProducts();
            return Ok(result);
        }


        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(ProductModel), 200)]
        public async Task<IActionResult> GetPhotoShootById(string id)
        {
            if( !Guid.TryParse(id, out var productId) )
                return BadRequest("id is required");
            var result = await _productService.GetProductById(productId);
            if( result == null )
                return NotFound();
            return Ok(result);
        }
    }
}
