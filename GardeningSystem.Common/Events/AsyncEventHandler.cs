using System;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Events {

    public delegate Task AsyncEventHandler<T>(object sender, T e);

    public delegate Task AsyncEventHandler(object sender, EventArgs e);
}
