using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkingLotLLD.Manager;
using ParkingLotLLD.Model;

namespace ParkingLotLLD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntranceGateController : ControllerBase
    {
        private readonly ParkingSpotManager _parkingSpotManager;
        public EntranceGateController(TwoWheelerPsManager parkingSpotManager)
        {
            _parkingSpotManager = parkingSpotManager;
        }
        
        [HttpGet]
        [Route("findPs")]
        public ParkingSpot FindParkingSpot(){
            return _parkingSpotManager.FindParkingSpace();
        }
    }
}
