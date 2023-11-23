using DistributedLocks.DistributedLock;
using DistributedLocks.NetworkDesign;
using Microsoft.AspNetCore.Mvc;
using System;

namespace DistributedLocks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NetworksController : ControllerBase
    {
        private readonly ILogger<NetworksController> _logger;
        private readonly INetworkManager _networkManager;
        private readonly IDistributedLock _distributedLock;

        public NetworksController(ILogger<NetworksController> logger,
            INetworkManager networkManager,
            IDistributedLock distributedLock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            this._distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
        }

        [HttpPost("CreateNetwork")]
        public IActionResult CreateNetwork()
        {
            // No lock acquired here
            var network = _networkManager.Create();
            return Ok(network);
        }

        [HttpGet("GetAllNetworks")]
        public IActionResult GetAllNetworks()
        {
            // Lets say we're not using locking here
            // Else, we may need a global lock

            var networks = this._networkManager.GetNetworks();

            if (networks.Any())
            {
                return Ok(networks);
            }

            return Ok("Nothing here");
        }

        [HttpGet("GetNetworkSlow")]
        public async Task<IActionResult> GetNetworkSlow(Guid guid)
        {
            await _distributedLock.AcquireReadLock(guid);

            Thread.Sleep(20 * 1000); // Spend some time ..

            var network = _networkManager.Get(guid);

            await _distributedLock.ReleaseReadLock(guid);

            if (network != null)
            {
                return Ok(network);
            }

            return Ok("Nothing here");
        }

        [HttpGet("GetNetworkFast")]
        public async Task<IActionResult> GetNetworkFast(Guid guid)
        {
            await _distributedLock.AcquireReadLock(guid);

            //Thread.Sleep(20 * 1000); // Dont Spend some time ..

            var network = _networkManager.Get(guid);

            await _distributedLock.ReleaseReadLock(guid);

            if (network != null)
            {
                return Ok(network);
            }

            return Ok("Nothing here");
        }

        [HttpPut("UpdateNetworkFast")]
        public async Task<IActionResult> UpdateNetworkFast(Guid guid)
        {
            await _distributedLock.AcquireWriteLock(guid);

            //Thread.Sleep(20 * 1000); // Intensive work for 20 sec

            var success = _networkManager.UpdateName(guid, 1000);

            await _distributedLock.ReleaseWriteLock(guid);

            return Ok(success);
        }

        [HttpPut("UpdateNetworkSlow")]
        public async Task<IActionResult> UpdateNetworkSlow(Guid guid)
        {
            await _distributedLock.AcquireWriteLock(guid);

            Thread.Sleep(20 * 1000); // Intensive work for 20 sec

            var success = _networkManager.UpdateName(guid, 1000);

            await _distributedLock.ReleaseWriteLock(guid);

            return Ok(success);
        }
    }
}