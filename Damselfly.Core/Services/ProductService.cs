using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class ProductService(ILogger<ProductService> logger,
        ImageContext imageContext,
        IMapper mapper,
        IAuthService authService)
    {
        private readonly ILogger<ProductService> _logger = logger;
        private readonly ImageContext _imageContext = imageContext;
        private readonly IMapper _mapper = mapper;
        private readonly IAuthService _authService = authService;

        public async Task<ProductModel> CreateProduct(ProductModel product)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to CreateProduct");
                return null;
            }
            _logger.LogInformation("Creating Product for {productName}, {productId} by request of {userEmail}",
                product.Name, product.ProductId, currentUserEmail);
            var dbProduct = _mapper.Map<Product>(product);
            _imageContext.Products.Add(dbProduct);
            await _imageContext.SaveChangesAsync();
            return _mapper.Map<ProductModel>(dbProduct);
        }

        public async Task<ProductModel> UpdateProduct(ProductModel product)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to UpdateProduct {productId}", product.ProductId);
                return null;
            }
            _logger.LogInformation("Updating Product {productId} by request of {userEmail}", product.ProductId, currentUserEmail);

            var dbProduct = _mapper.Map<Product>(product);
            _imageContext.Products.Update(dbProduct);
            await _imageContext.SaveChangesAsync();
            return _mapper.Map<ProductModel>(dbProduct);
        }

        public async Task<bool> DeleteProduct(Guid id)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to DeleteProduct {productId}", id);
                return false;
            }
            _logger.LogInformation("Deleting Product {productId} by request of {userEmail}", id, currentUserEmail);
            var product = await _imageContext.Products.FindAsync(id);
            if( product == null ) return false;
            product.IsDeleted = true;
            await _imageContext.SaveChangesAsync();
            return true;
        }

        public async Task<ProductModel> GetProductById(Guid id)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to GetProductById {productId}", id);
                return null;
            }
            _logger.LogInformation("Getting Product {productId} by request of {userEmail}", id, currentUserEmail);

            var dbProduct = await _imageContext.Products.FindAsync(id);
            if( dbProduct == null ) return null;
            return _mapper.Map<ProductModel>(dbProduct);
        }

        public async Task<List<ProductModel>> GetProducts()
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to GetProducts");
                return null;
            }
            _logger.LogInformation("Getting Products by request of {userEmail}", currentUserEmail);

            var products = await _imageContext.Products.Where(x => !x.IsDeleted).ToListAsync();

            return products.Select(_mapper.Map<ProductModel>).ToList();
        }
    }
}
