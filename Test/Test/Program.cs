using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    [DataContract]
    public class User
    {
        [DataMember(Name = "loginname")]
        public string Name { get; set; }
        [DataMember(Name = "password")]
        public string Password { get; set; }
    }
class Program
    {
        static void Main(string[] args)
        {
            User user = new User() { Name = "天使", Password = "2016-11-05" };
            if(Utilities.Serialization.SerializeJsonObject(user, out string bookString, out string errorMessage))
            {
                string response = Utilities.HttpRequest.GetHttpResponseToText2("http://localhost:8080/hello", null, null, null, 50000, null, true);
            }
        }
    }
}
