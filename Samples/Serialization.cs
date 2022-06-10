using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples
{
    public class SomeObjectWithArray
    {
        public object[] array;
    }

    public class A
    {
        public int Number;
    }

    public class B
    {
        public int Number;
    }

    internal class Serialization
    {
        public void Run()
        {
            SomeObjectWithArray obj = new SomeObjectWithArray();
            obj.array = new object[] { new A(), new B() };
            foreach (object item in obj.array)
            {
                Console.WriteLine($"Type: {item.GetType()} - {item}");
            }

            //string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };
            string json = JsonConvert.SerializeObject(obj, settings);

            Console.WriteLine(json);

            SomeObjectWithArray objFromJson = JsonConvert.DeserializeObject<SomeObjectWithArray>(json, settings);
            foreach (object item in objFromJson.array)
            {
                Console.WriteLine($"Type: {item.GetType()} - {item}");
            }
        }
    }
}
