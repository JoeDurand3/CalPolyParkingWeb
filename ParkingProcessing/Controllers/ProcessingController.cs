using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using ParkingProcessing.Entities;
using ParkingProcessing.Services;

namespace ParkingProcessing.Controllers
{
    [Route("api/processing")]
    public class ProcessingController : Controller
    {
        // GET api/values
        [HttpGet]
        public string Get()
        {
            return "";
        }

        // POST api/values
        //[Route("{testValue}")]
        public IActionResult Post([FromBody]ParkingLotData data)//, string testValue)
        {
            Boolean configRequired = false;
            //if (!Boolean.TryParse(testValue, out configRequired))
            {
          //      PseudoLoggingService.Log("ProcessingController", "Configuration request is defaulting to false");
            }
            
            try
            {
                ProcessingService.Instance.AcceptParkingLotData(data);
                PseudoLoggingService.Log("ProcessingController", "Spots " + data.ParkingSpots.First().id + " - " + data.ParkingSpots.Last().id + " accepted.");
                return Ok(); //value: configRequired);
            }
            catch (Exception e)
            {
                PseudoLoggingService.Log("ProcessingController", e);
            }
            
            return BadRequest();
        }
    }
}