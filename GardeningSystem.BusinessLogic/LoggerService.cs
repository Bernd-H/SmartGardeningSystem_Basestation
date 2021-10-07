﻿using System;
using System.Collections.Generic;
using GardeningSystem.Common.Specifications;
using NLog;

namespace GardeningSystem.BusinessLogic {
    public class LoggerService : ILoggerService {

        private Dictionary<Type, ILogger> logger; // to allow to return new logger instances for different classes

        public LoggerService() {
            logger = new Dictionary<Type, ILogger>();
        }

        public ILogger GetLogger<T>() where T : class {
            if (!logger.ContainsKey(typeof(T))) {
                logger.Add(typeof(T), LogManager.GetLogger(typeof(T).FullName));
            }

            return logger[typeof(T)];
        }
    }
}
