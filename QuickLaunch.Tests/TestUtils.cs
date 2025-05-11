using QuickLaunch.Core.Utils.QuotedString;

namespace QuickLaunch.Tests.Utils
{
    sealed class Defs
    {
        public const string QUOTED_DQ = "\\\"";
        public const string QUOTED_BS = "\\\\";
        public const string DQ = "\"";
        public string ESCAPE(char c) => $"\\{c}";
    }

    [TestClass]
    public sealed class TestQuotedConverter
    {
        [TestMethod]
        public void TestDefault()
        {
            var quotedStringConverter = new QuotedStringTypeConverter();

            var qsToString = new (string, string)[]
            {
                (@"quoted", @"""quoted"""),
                (@"quoted """, @"""quoted " + Defs.QUOTED_DQ + Defs.DQ),
                (@"quoted [", @$"""quoted [""")
            };

            foreach (var (unquoted, quoted) in qsToString)
            {
                Assert.AreEqual(quoted, quotedStringConverter.ConvertToInvariantString(new QuotedString(unquoted)));
                var q = (QuotedString?)quotedStringConverter.ConvertFrom(quoted);
                Assert.AreEqual(unquoted, q?.Value);
            }

            var formatErrors = new[] {
                    (@"""test """"", 7, true),
                    (@"""test", 5, false),
                    (@"test """, 0, false)
                };

            foreach (var (quoted, expConsumed, partialSuccess) in formatErrors)
            {
                Assert.ThrowsException<FormatException>(() => quotedStringConverter.ConvertFrom(quoted), quoted);
                Assert.AreEqual(partialSuccess,
                    quotedStringConverter.TryParsePrefix(quoted, out var parsed, out int consumed));
                Assert.AreEqual(expConsumed, consumed);
            }
        }

        [TestMethod]
        public void TestCustom()
        {
            var quotedStringConverter = new QuotedStringTypeConverter(new[] {
                new Escape('['), new(']', ']')
            });

            var qsToString = new (string, string)[]
            {
                (@"test", @"""test"""),
                (@"test """, @"""test " + Defs.QUOTED_DQ + Defs.DQ),
                (@"test [", @$"""test \["""),
                (@"test []", @$"""test \[\]"""),
                (@"test \n", @$"""test {Defs.QUOTED_BS}n"""),
                ("test \n", "\"test \n\""),
            };

            foreach (var (unquoted, quoted) in qsToString)
            {
                var q = quotedStringConverter.ConvertToInvariantString(new QuotedString(unquoted));
                Assert.AreEqual(quoted, q);
                var uq = ((QuotedString?)quotedStringConverter.ConvertFrom(quoted))?.Value;
                Assert.AreEqual(unquoted, uq);
            }

            var formatErrors = new[] {
                "test",
                @"""test """"",
                @"""test",
                @"test """,
                @"""test [""",
            };

            foreach (var quoted in formatErrors)
            {
                Assert.ThrowsException<FormatException>(() => quotedStringConverter.ConvertFrom(quoted), quoted);
            }
        }

        [TestMethod]
        public void TestCustom2()
        {
            var quotedStringConverter = new QuotedStringTypeConverter(new[] {
                new Escape( '\n', 'n' ), new ('\t', 't', AllowUnescaped: true)
            });

            var qsToString = new (string, string)[]
            {
                (@"quoted", @"""quoted"""),
                (@"quoted """, @"""quoted " + Defs.QUOTED_DQ + Defs.DQ),
                (@"quoted [", @$"""quoted ["""),
                (@"quoted []", @$"""quoted []"""),
                (@"quoted \n", @$"""quoted {Defs.QUOTED_BS}n"""),
                ("quoted \n", @$"""quoted \n"""),
            };

            foreach (var (unquoted, quoted) in qsToString)
            {
                Assert.AreEqual(quoted, quotedStringConverter.ConvertToInvariantString(new QuotedString(unquoted)));
                var q = (QuotedString?)quotedStringConverter.ConvertFrom(quoted);
                Assert.AreEqual(unquoted, q?.Value);
            }

            var qsFromString = new (string, string)[] {
                ("\"test \t\"", "test \t"),
            };
            foreach (var (quoted, unquoted) in qsFromString)
            {
                var q = (QuotedString?)quotedStringConverter.ConvertFrom(quoted);
                Assert.AreEqual(unquoted, q?.Value);
            }

            var formatErrors = new[] {
                "test",
                @"""test """"",
                @"""test",
                @"test """,
                "\"test \n\"",
            };

            foreach (var quoted in formatErrors)
            {
                Assert.ThrowsException<FormatException>(() => quotedStringConverter.ConvertFrom(quoted), quoted);
            }
        }
    }

    [TestClass]
    public sealed class TestMaybeQuotedConverter
    {
        [TestMethod]
        public void TestDefault()
        {
            var mqsConverter = new MaybeQuotedStringTypeConverter();

            var qsToString = new (string, string)[]
            {
                (@"test", @"test"),
                (@"test """, @"""test " + Defs.QUOTED_DQ + Defs.DQ),
                (@"test [", @$"""test ["""),
                (@"test [", @$"""test ["""),
                (@"test []", @$"""test []"""),
                (@"test \n", @$"""test {Defs.QUOTED_BS}n"""),
                ("test \n", "\"test \n\""),
            };
            var qsFromString = new (string, string)[]
            {
                (@"""test""", @"test"),
                (@"test", @"test"),
                (@"""test " + Defs.QUOTED_DQ + Defs.DQ, @"test """),
                (@"""test [""", @"test ["),
                (@"""test \[""", @"test \["),
                (@"""test \\[""", @"test \["),
                (@"""test \\\[""", @"test \\["),
                (@$"""test {Defs.QUOTED_BS}n""", @"test \n"),
                ("\"test \\n\"", "test \\n"),
            };

            foreach (var (unquoted, quoted) in qsToString)
            {
                Assert.AreEqual(quoted, mqsConverter.ConvertToInvariantString(new MaybeQuotedString(unquoted)));
            }
            foreach (var (quoted, unquoted) in qsFromString)
            {
                var q = (MaybeQuotedString?)mqsConverter.ConvertFrom(quoted);
                Assert.AreEqual(unquoted, q?.Value);
            }

            var formatErrors = new[] {
                (@"""test """"", 7, true),
                (@"""test", 5, false),
                (@"test """, 4, true),
                (@"test""", 4, false),
                (@"test\", 4, false),
                (@"test ", 4, true)
             };

            foreach (var (quoted, expConsumed, partialSuccess) in formatErrors)
            {
                Assert.ThrowsException<FormatException>(() => mqsConverter.ConvertFrom(quoted), quoted);
                Assert.AreEqual(partialSuccess,
                    mqsConverter.TryParsePrefix(quoted, out var parsed, out int consumed));
                Assert.AreEqual(expConsumed, consumed);
            }
        }

        [TestMethod]
        public void TestCustom()
        {
            {
                var mqsConverter = new MaybeQuotedStringTypeConverter(new[] {
                    new Escape('[', AllowUnescaped: true), new(']', ']', AllowUnescaped: true)});

                var qsToString = new (string, string)[]
                {
                    (@"test", @"test"),
                    (@"test """, @"""test " + Defs.QUOTED_DQ + Defs.DQ),
                    (@"test [", @$"""test \["""),
                    (@"test []", @$"""test \[\]"""),
                    (@"test \n", @$"""test {Defs.QUOTED_BS}n"""),
                    ("test \n", "\"test \n\""),
                    };
                var qsFromString = new (string, string)[] {
                    (@"""test""", @"test"),
                    (@"test", @"test"),
                    (@"""test " + Defs.QUOTED_DQ + Defs.DQ, @"test """),
                    (@"""test [""", @"test ["),
                    (@"""test \[""", @"test ["),
                    (@"""test \\[""", @"test \["),
                    (@"""test \\\[""", @"test \["),
                    (@$"""test {Defs.QUOTED_BS}n""", @"test \n"),
                    ("\"test \\n\"", "test \\n"),
                };

                foreach (var (unquoted, quoted) in qsToString)
                {
                    var q = mqsConverter.ConvertToInvariantString(new MaybeQuotedString(unquoted));
                    Assert.AreEqual(quoted, q);
                }
                foreach (var (quoted, unquoted) in qsFromString)
                {
                    var q = (MaybeQuotedString?)mqsConverter.ConvertFrom(quoted);
                    Assert.AreEqual(unquoted, q?.Value);
                }
            }
            {
                var mqsConverter = new MaybeQuotedStringTypeConverter(new[] {
                    new Escape('['), new(']', ']')});
                var qsFromString = new (string, string)[] {
                    (@"""test""", @"test"),
                    (@"test", @"test"),
                    (@"""test " + Defs.QUOTED_DQ + Defs.DQ, @"test """),
                    (@"""test \[""", @"test ["),
                    (@"""test \\\[""", @"test \["),
                    (@$"""test {Defs.QUOTED_BS}n""", @"test \n"),
                    ("\"test \\n\"", "test \\n"),
                };
                foreach (var (quoted, unquoted) in qsFromString)
                {
                    var q = (MaybeQuotedString?)mqsConverter.ConvertFrom(quoted);
                    Assert.AreEqual(unquoted, q?.Value);
                }
                var formatErrors = new[] {
                    @"""test """"",
                    @"""test",
                    @"test""",
                    @"test\",
                    @"""test [""",
                    @"""test \\[""",
                    @"test[",
                    @"test ",
                };

                foreach (var quoted in formatErrors)
                {
                    Assert.ThrowsException<FormatException>(() => mqsConverter.ConvertFrom(quoted), quoted);
                }
            }
        }

        [TestMethod]
        public void TestCustom2()
        {
            var mqsConverter = new MaybeQuotedStringTypeConverter(new[] {
                new Escape('\n', 'n'), new ('\t', 't')
            });

            var qsToString = new (string, string)[]
            {
                (@"test", @"test"),
                (@"test """, @"""test " + Defs.QUOTED_DQ + Defs.DQ),
                (@"test [", @$"""test ["""),
                (@"test []", @$"""test []"""),
                (@"test \n", @$"""test {Defs.QUOTED_BS}n"""),
                ("test \n", "\"test \\n\""),
            };
            var qsFromString = new (string, string)[] {
                (@"""test""", @"test"),
                (@"test", @"test"),
                (@"""test " + Defs.QUOTED_DQ + Defs.DQ, @"test """),
                (@"""test [""", @"test ["),
                (@"""test \[""", @"test \["),
                (@"""test \\[""", @"test \["),
                (@"""test \\\[""", @"test \\["),
                (@$"""test {Defs.QUOTED_BS}n""", @"test \n"),
                ("\"test \\n\"", "test \n"),
            };

            foreach (var (unquoted, quoted) in qsToString)
            {
                var q = mqsConverter.ConvertToInvariantString(new MaybeQuotedString(unquoted));
                Assert.AreEqual(quoted, q);
            }
            foreach (var (quoted, unquoted) in qsFromString)
            {
                var q = (MaybeQuotedString?)mqsConverter.ConvertFrom(quoted);
                Assert.AreEqual(unquoted, q?.Value);
            }

            var formatErrors = new[] {
                @"""test """"",
                @"""test",
                @"test """,
                @"test""",
                @"test\",
                @"test ",
                "\"test \n\"",
                "\"test \t\"",
                "test\n"
            };

            foreach (var quoted in formatErrors)
            {
                Assert.ThrowsException<FormatException>(() => mqsConverter.ConvertFrom(quoted), quoted);
            }
        }
    }

    [TestClass]
    public sealed class TestMQListConverter
    {
        [TestMethod]
        public void TestDefault()
        {
            var converter = new MaybeQuotedStringListTypeConverter();

            var tests = new[]
            {
                (
                    @"[""test"", test]",
                    new[] {@"test", @"test" },
                    @"[test, test]"
                )
            };

            foreach (var (quoted, unquoted, canonical) in tests)
            {
                var uqTemp = ((List<string>?)converter.ConvertFrom(quoted));
                string[]? uq = uqTemp?.ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(unquoted, uq));

                Assert.AreEqual(canonical, converter.ConvertToInvariantString(unquoted));
            }
        }
    }
}
