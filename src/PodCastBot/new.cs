using system;
using Microsoft.Extensions.Logging;

namespace PodCastBot
{
    public class smth{
       Logging log= AppLog.LoggerFactory.CreateLogger("testClassLog");
       public smth(){
log.LogCritical("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE"+ this.GetType().ToString());
       } 
    }
}