﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ER.Shared.Services.Logging;
using Serilog;
using SPNR.Core.Models;
using SPNR.Core.Models.Works;
using SPNR.Core.Services.Data.Contexts;
using SPNR.Core.Services.Selection.Drivers;

namespace SPNR.Core.Services.Selection
{
    public class SelectionService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ScWorkContext _dbContext;
        private readonly Dictionary<string, ISelectionDriver> _drivers = new();
        private readonly ILogger _logger;

        public SelectionService(ILoggerFactory loggerFactory, ScWorkContext dbContext)
        {
            _loggerFactory = loggerFactory;
            _dbContext = dbContext;
            _logger = loggerFactory.GetLogger("Sel");
        }

        public IReadOnlyCollection<string> GetLoadedDrivers()
        {
            return new List<string>(_drivers.Keys);
        }

        public void Initialize()
        {
            _logger.Verbose("Loading drivers");

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISelectionDriver).IsAssignableFrom(p) &&
                            p.GetCustomAttributes(typeof(SelectionDriver), true).Length > 0);

            foreach (var type in types)
            {
                var name = ((SelectionDriver) type.GetCustomAttributes(typeof(SelectionDriver), true)[0]).Name;
                if (_drivers.ContainsKey(name))
                {
                    _logger.Error($"Driver for \"{name}\" is already loaded");
                    continue;
                }

                _drivers.Add(name, (ISelectionDriver) Activator.CreateInstance(type, _loggerFactory));
                _logger.Verbose($"Loaded driver for \"{name}\"");
            }
        }

        public async Task<List<ScientificWork>> Search(SearchInfo searchInfo)
        {
            var works = new List<ScientificWork>();

            _logger.Verbose("Start searching for works");

            foreach (var (driverId, driver) in _drivers)
            {
                _logger.Verbose($"Searching at: \"{driverId}\"");
                await driver.Initialize();
                works.AddRange(await driver.Search(searchInfo));
            }

            
            
            return works;
        }
    }
}