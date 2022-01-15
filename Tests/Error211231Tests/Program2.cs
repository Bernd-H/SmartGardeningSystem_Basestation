using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Error211231Tests {
    public class Program2 {

        private EventHandler eventHandler;

        public Program2() {
            eventHandler += OnEventHandler;

            eventHandler.Invoke(null, null);

            Console.WriteLine("EventHandler finished.");
            Console.ReadLine();

            /// result:
            /// In EventHandler
            /// EventHandler finished.
            /// In EventHandler 2
        }

        public async void OnEventHandler(object o, EventArgs e) {
            Console.WriteLine("In EventHandler");
            await Task.Delay(5000);
            Console.WriteLine("In EventHandler 2");
        }
    }
}
