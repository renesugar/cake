﻿using System.Collections.Generic;
using System.Linq;
using Cake.Core.Graph;
using Cake.Core.IO;

namespace Cake.Core
{
    public sealed class CakeEngine : ICakeEngine
    {        
        private readonly IFileSystem _fileSystem;
        private readonly ICakeEnvironment _environment;
        private readonly IGlobber _globber;
        private readonly List<CakeTask> _tasks;

        public IFileSystem FileSystem
        {
            get { return _fileSystem; }
        }

        public ICakeEnvironment Environment
        {
            get { return _environment; }
        }

        public IReadOnlyList<CakeTask> Tasks
        {
            get { return _tasks; }
        }

        public IGlobber Globber
        {
            get { return _globber; }
        }

        public CakeEngine()
            : this(null, null)
        {
        }

        public CakeEngine(IFileSystem fileSystem, ICakeEnvironment environment)
        {            
            _fileSystem = fileSystem ?? new FileSystem();
            _environment = environment ?? new CakeEnvironment();
            _globber = new Globber(_fileSystem, _environment);
            _tasks = new List<CakeTask>();
        }

        public CakeTask Task(string name)
        {
            if (_tasks.Any(x => x.Name == name))
            {
                const string format = "Another task with the name '{0}' has already been added.";
                throw new CakeException(string.Format(format, name));
            }
            var task = new CakeTask(name);
            _tasks.Add(task);
            return task;
        }

        public void Run(string target)
        {
            var graph = CakeGraphBuilder.Build(_tasks);
            var context = CreateContext();
            foreach (var task in graph.Traverse(target))
            {
                if (ShouldTaskExecute(task, context))
                {
                    foreach (var action in task.Actions)
                    {
                        action(context);
                    }
                }
            }
        }

        private CakeContext CreateContext()
        {
            var context = new CakeContext(_fileSystem, _environment, _globber);
            return context;
        }

        private static bool ShouldTaskExecute(CakeTask task, ICakeContext context)
        {
            foreach (var criteria in task.Criterias)
            {
                if (!criteria(context))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
