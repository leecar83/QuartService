using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace QuartzService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			#if (!DEBUG)
					   ServiceBase[] ServicesToRun;
					   ServicesToRun = new ServiceBase[] 
				   { 
						new QuartzService() 
				   };
					   ServiceBase.Run(ServicesToRun);
			#else
			QuartzService myServ = new QuartzService();
			myServ.onStartMethod();
#endif
		}
	}
}
