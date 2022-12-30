﻿using NUnit.Framework;

namespace ulox.core.tests
{
    public class EngineTestBase
    {
        protected ByteCodeInterpreterTestEngine testEngine;

        [SetUp]
        public virtual void Setup()
        {
            testEngine = new ByteCodeInterpreterTestEngine(System.Console.WriteLine);
        }
    }
}