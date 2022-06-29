using CustomerWebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace CustomerWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly IDistributedCache _cache;
        public CustomerController(ILogger<CustomerController> logger,
            IDistributedCache cache,
            CustomerDbContext db)
        {
            _logger = logger;
            _customerDbContext = db;
            _cache = cache;
        }
        private readonly CustomerDbContext _customerDbContext;

        [HttpGet]
        public ActionResult<IEnumerable<Customer>> GetCustomers()
        {
            List<Customer> customerList = new();
            var cachedCustomer = _cache.GetString("customerList");
            if (!string.IsNullOrEmpty(cachedCustomer))
            {
                //cache
                #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                                customerList = JsonConvert.DeserializeObject<List<Customer>>(cachedCustomer);
                #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            }
            else
            {
                customerList = _customerDbContext.Customers.ToList();
                DistributedCacheEntryOptions options = new();
                options.SetAbsoluteExpiration(new TimeSpan(0, 0, 30));

                _cache.SetString("customerList", JsonConvert.SerializeObject(customerList), options);
            }
            return customerList;
        }

        [HttpGet("{customerId:int}")]
        public async Task<ActionResult<Customer>> GetById(int customerId)
        {
            var customer = await _customerDbContext.Customers.FindAsync(customerId);
            return customer;
        }

        [HttpPost]
        public async Task<ActionResult> Create(Customer customer)
        {
            await _customerDbContext.Customers.AddAsync(customer);
            await _customerDbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPut]
        public async Task<ActionResult> Update(Customer customer)
        {
            _customerDbContext.Customers.Update(customer);
            await _customerDbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{customerId:int}")]
        public async Task<ActionResult> Delete(int customerId)
        {
            var customer = await _customerDbContext.Customers.FindAsync(customerId);
            _customerDbContext.Customers.Remove(customer);
            await _customerDbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
