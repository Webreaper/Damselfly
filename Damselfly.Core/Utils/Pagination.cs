using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Utils
{
    public static class Pagination
    {
        /// <summary>
        /// Paginates a pre-created query and puts it in a PaginatedReult object. Query must be IOrderedQueryable, so make sure to use an
        /// orderby clause. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="P"></typeparam>
        /// <param name="query">The query defined to pull data</param>
        /// <param name="pageNumber">0 index page number</param>
        /// <param name="pageSize">Number of entries on a page</param>
        /// <param name="builder">A constructor that takes type P and returns type T used to mutate the result of the query</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<PaginationResultModel<T>> PaginateQuery<T, P>(IOrderedQueryable<P> query, int pageNumber, int pageSize, Func<P, T> builder)
        {
            if( pageSize <= 0 )
            {
                throw new ArgumentException("Page Size must be greater than zero");
            }
            if( pageNumber < 0 )
            {
                throw new ArgumentException("Page number must be greater than or equal to zero");
            }

            var recordCount = query.Count();
            var resultSet = await query
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .Select(value => builder(value))
                .ToListAsync();

            var paginatedResults = new PaginationResultModel<T>
            {
                PageCount = (int)Math.Ceiling((double)recordCount / pageSize),
                PageSize = pageSize,
                PageIndex = pageNumber,
                Results = resultSet,
                TotalCount = recordCount
            };
            return paginatedResults;
        }
    }
}
