// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class CachingReaderTests : TestBase
    {
        #region Close

        [Fact]
        public void Close_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.Close());
        }

        [Fact]
        public void IsClosed_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => { var x = reader.IsClosed; });
        }

        [Fact]
        public void Close_on_closed_reader_does_not_throw()
        {
            using (var reader = CreateCachingReader())
            {
                reader.Close();
                reader.Close();
                Assert.True(reader.IsClosed);
            }
        }

        [Fact]
        public void Close_closes_reader()
        {
            using (var reader = CreateCachingReader())
            {
                Assert.False(reader.IsClosed);
                reader.Close();
                Assert.True(reader.IsClosed);
            }
        }

        #endregion

        #region Read

        [Fact]
        public void Read_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.Read());
        }

        [Fact]
        public void Read_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.Read());
        }

        [Fact]
        public void Read_returns_false_when_no_rows_to_read()
        {
            using (var reader = CreateCachingReader())
            {
                Assert.False(reader.Read());
            }
        }

        [Fact]
        public void Read_returns_false_only_after_all_rows_read()
        {
            using (var reader = CreateCachingReader(3))
            {
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.False(reader.Read());
            }
        }

        #endregion

        #region Schema table

        [Fact]
        public void FieldCount_returns_number_of_fields()
        {
            using (var reader = CreateCachingReader(2))
            {
                Assert.Equal(2, reader.FieldCount);
            }
        }

        [Fact]
        public void GetDataTypeName_returns_type_name_for_the_given_ordinal()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata(null, "abc", null),
                new ColumnMetadata(null, "123", null),
                new ColumnMetadata(null, "!@#", null),
            };

            using (var reader = new CachingReader(new CachedResults(tableMetadata, new List<object[]>(), 0)))
            {
                Assert.Equal("abc", reader.GetDataTypeName(0));
                Assert.Equal("123", reader.GetDataTypeName(1));
                Assert.Equal("!@#", reader.GetDataTypeName(2));
            }
        }

        [Fact]
        public void GetDataTypeName_throws_if_index_out_of_range()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata(null, "abc", null),
            };

            using (var reader = new CachingReader(new CachedResults(tableMetadata, new List<object[]>(), 0)))
            {
                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDataTypeName(-1));
                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDataTypeName(1));
            }
        }

        [Fact]
        public void GetFieldType_returns_type_for_the_given_ordinal()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata(null, null, typeof(int)),
                new ColumnMetadata(null, null, typeof(string)),
                new ColumnMetadata(null, null, typeof(object)),
            };

            using (var reader = new CachingReader(new CachedResults(tableMetadata, new List<object[]>(), 0)))
            {
                Assert.Same(typeof(int), reader.GetFieldType(0));
                Assert.Same(typeof(string), reader.GetFieldType(1));
                Assert.Same(typeof(object), reader.GetFieldType(2));
            }
        }

        [Fact]
        public void GetFieldType_throws_if_index_out_of_range()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata(null, null, typeof(string)),
            };

            using (var reader = new CachingReader(new CachedResults(tableMetadata, new List<object[]>(), 0)))
            {
                Assert.Throws<IndexOutOfRangeException>(() => reader.GetFieldType(-1));
                Assert.Throws<IndexOutOfRangeException>(() => reader.GetFieldType(1));
            }
        }

        [Fact]
        public void GetName_returns_name_for_the_given_ordinal()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata("Z", null, null),
                new ColumnMetadata("X", null, null),
                new ColumnMetadata("Y", null, null),
            };

            using (var reader = new CachingReader(new CachedResults(tableMetadata, new List<object[]>(), 0)))
            {
                Assert.Same("Z", reader.GetName(0));
                Assert.Same("X", reader.GetName(1));
                Assert.Same("Y", reader.GetName(2));
            }
        }

        [Fact]
        public void GetName_throws_if_index_out_of_range()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata("Z", null, null),
            };

            using (var reader = new CachingReader(new CachedResults(tableMetadata, new List<object[]>(), 0)))
            {
                Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(-1));
                Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(1));
            }
        }

        [Fact]
        public void Can_get_RecordsAffected()
        {
            Assert.Equal(42, 
                new CachingReader(
                    new CachedResults(
                        new ColumnMetadata[0], new List<object[]>(), 42)).RecordsAffected);
        }

        #endregion

        #region IsDBNull

        [Fact]
        public void IsDBNull_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.IsDBNull(0));
        }

        [Fact]
        public void IsDBNull_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.IsDBNull(0));
        }

        [Fact]
        public void IsDBNull_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.IsDBNull(0));
        }

        [Fact]
        public void IsDBNull_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.IsDBNull(0));
        }

        [Fact]
        void IsDBNull_throws_for_negative_column_index()
        {
            using(var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.IsDBNull(-1));
            }
        }

        [Fact]
        void IsDBNull_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.IsDBNull(100));
            }
        }

        [Fact]
        void IsDBNull_returns_true_for_null_column_value()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.True(reader.IsDBNull(1));
            }
        }

        [Fact]
        void IsDBNull_returns_true_for_non_null_column_value()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.False(reader.IsDBNull(0));
            }
        }

        #endregion

        #region GetValue

        [Fact]
        public void GetValue_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetValue(0));
        }

        [Fact]
        public void GetValue_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetValue(0));
        }

        [Fact]
        public void GetValue_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetValue(0));
        }

        [Fact]
        public void GetValue_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetValue(0));
        }
        
        [Fact]
        void GetValue_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetValue(-1));
            }
        }

        [Fact]
        void GetValue_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetValue(100));
            }
        }

        [Fact]
        void GetValue_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal("0", reader.GetValue(2));
            }
        }
        
        #endregion

        #region GetInt32

        [Fact]
        public void GetInt32_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetInt32(0));
        }

        [Fact]
        public void GetInt32_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetInt32(0));
        }

        [Fact]
        public void GetInt32_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetInt32(0));
        }

        [Fact]
        public void GetInt32_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetInt32(0));
        }

        
        [Fact]
        void GetInt32_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetInt32(-1));
            }
        }

        [Fact]
        void GetInt32_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetInt32(100));
            }
        }

        [Fact]
        void GetInt32_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(0, reader.GetInt32(0));
            }
        }

        #endregion

        #region GetString

        [Fact]
        public void GetString_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetString(0));
        }

        [Fact]
        public void GetString_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetString(0));
        }

        [Fact]
        public void GetString_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetString(0));
        }

        [Fact]
        public void GetString_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetString(0));
        }

        [Fact]
        void GetString_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetString(-1));
            }
        }

        [Fact]
        void GetString_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetString(100));
            }
        }

        [Fact]
        void GetString_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal("0", reader.GetString(2));
            }
        }
       
        #endregion

        #region GetBoolean

        [Fact]
        public void GetBoolean_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetBoolean(0));
        }

        [Fact]
        public void GetBoolean_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetBoolean(0));
        }

        [Fact]
        public void GetBoolean_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetBoolean(0));
        }

        [Fact]
        public void GetBoolean_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetBoolean(0));
        }

        [Fact]
        void GetBoolean_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetBoolean(-1));
            }
        }

        [Fact]
        void GetBoolean_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetBoolean(100));
            }
        }

        [Fact]
        void GetBoolean_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.True(reader.GetBoolean(3));
            }
        }

        #endregion

        #region GetInt64

        [Fact]
        public void GetInt64_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetInt64(0));
        }

        [Fact]
        public void GetInt64_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetInt64(0));
        }

        [Fact]
        public void GetInt64_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetInt64(0));
        }

        [Fact]
        public void GetInt64_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetInt64(0));
        }

        [Fact]
        void GetInt64_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetInt64(-1));
            }
        }

        [Fact]
        void GetInt64_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetInt64(100));
            }
        }

        [Fact]
        void GetInt64_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(long.MaxValue, reader.GetInt64(4));
            }
        }
        
        #endregion

        #region GetInt16

        [Fact]
        public void GetInt16_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetInt16(0));
        }

        [Fact]
        public void GetInt16_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetInt16(0));
        }

        [Fact]
        public void GetInt16_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetInt16(0));
        }

        [Fact]
        public void GetInt16_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetInt16(0));
        }

        [Fact]
        void GetInt16_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetInt16(-1));
            }
        }

        [Fact]
        void GetInt16_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetInt16(100));
            }
        }

        [Fact]
        void GetInt16_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(short.MaxValue, reader.GetInt16(5));
            }
        }
        
        #endregion

        #region GetGuid

        [Fact]
        public void GetGuid_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetGuid(0));
        }

        [Fact]
        public void GetGuid_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetGuid(0));
        }

        [Fact]
        public void GetGuid_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetGuid(0));
        }

        [Fact]
        public void GetGuid_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetGuid(0));
        }

        [Fact]
        void GetGuid_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetGuid(-1));
            }
        }

        [Fact]
        void GetGuid_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetGuid(100));
            }
        }

        [Fact]
        void GetGuid_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(
                    new Guid("3CC55B83-2DE5-4DCE-9D31-1D306A82972A"), 
                    reader.GetGuid(6));
            }
        }
       
        #endregion

        #region GetFloat

        [Fact]
        public void GetFloat_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetFloat(0));
        }

        [Fact]
        public void GetFloat_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetFloat(0));
        }

        [Fact]
        public void GetFloat_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetFloat(0));
        }

        [Fact]
        public void GetFloat_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetFloat(0));
        }
        
        [Fact]
        void GetFloat_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetFloat(-1));
            }
        }

        [Fact]
        void GetFloat_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetFloat(100));
            }
        }

        [Fact]
        void GetFloat_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(3.5F, reader.GetFloat(7));
            }
        }
        
        #endregion

        #region GetDouble

        [Fact]
        public void GetDouble_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetDouble(0));
        }

        [Fact]
        public void GetDouble_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetDouble(0));
        }

        [Fact]
        public void GetDouble_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetDouble(0));
        }

        [Fact]
        public void GetDouble_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetDouble(0));
        }

        [Fact]
        void GetDouble_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDouble(-1));
            }
        }

        [Fact]
        void GetDouble_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDouble(100));
            }
        }

        [Fact]
        void GetDouble_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(5.5d, reader.GetDouble(8));
            }
        }

        #endregion

        #region GetDecimal

        [Fact]
        public void GetDecimal_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetDecimal(0));
        }

        [Fact]
        public void GetDecimal_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetDecimal(0));
        }

        [Fact]
        public void GetDecimal_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetDecimal(0));
        }

        [Fact]
        public void GetDecimal_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetDecimal(0));
        }

        [Fact]
        void GetDecimal_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDecimal(-1));
            }
        }

        [Fact]
        void GetDecimal_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDecimal(100));
            }
        }

        [Fact]
        void GetDecimal_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(21m, reader.GetDecimal(9));
            }
        }
       
        #endregion

        #region GetDateTime

        [Fact]
        public void GetDateTime_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetDateTime(0));
        }

        [Fact]
        public void GetDateTime_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetDateTime(0));
        }

        [Fact]
        public void GetDateTime_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetDateTime(0));
        }

        [Fact]
        public void GetDateTime_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetDateTime(0));
        }

        [Fact]
        void GetDateTime_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDateTime(-1));
            }
        }

        [Fact]
        void GetDateTime_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetDateTime(100));
            }
        }

        [Fact]
        void GetDateTime_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(
                    new DateTime(2001, 11, 17), 
                    reader.GetDateTime(10));
            }
        }

        #endregion

        #region GetByte

        [Fact]
        public void GetByte_throws_if_called_on_disposed_reader()
        {
            Action_throws_if_called_on_disposed_reader((reader) => reader.GetByte(0));
        }

        [Fact]
        public void GetByte_throws_if_called_on_closed_reader()
        {
            Action_throws_if_called_on_closed_reader((reader) => reader.GetByte(0));
        }

        [Fact]
        public void GetByte_throws_if_called_before_first_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetByte(0));
        }

        [Fact]
        public void GetByte_throws_if_called_after_all_rows_read()
        {
            Action_throws_if_called_before_first_read((reader) => reader.GetByte(0));
        }

        [Fact]
        void GetByte_throws_for_negative_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetByte(-1));
            }
        }

        [Fact]
        void GetByte_throws_for_invalid_column_index()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Throws<IndexOutOfRangeException>(() => reader.GetByte(100));
            }
        }

        [Fact]
        void GetByte_returns_actual_value_for_column()
        {
            using (var reader = CreateCachingReader(1))
            {
                var result = reader.Read();
                Assert.True(result);

                Assert.Equal(15, reader.GetByte(11));
            }
        }

        #endregion
        /*
            GetChars
            GetBytes
         */
        
        private static void Action_throws_if_called_on_disposed_reader(Action<CachingReader> action)
        {
            var reader = CreateCachingReader();
            reader.Dispose();
            Assert.Throws<InvalidOperationException>(() => action(reader));
        }

        private static void Action_throws_if_called_on_closed_reader(Action<CachingReader> action)
        {
            using (var reader = CreateCachingReader())
            {
                reader.Close();
                Assert.Throws<InvalidOperationException>(() => action(reader));
            }
        }

        private static void Action_throws_if_called_before_first_read(Action<CachingReader> action)
        {
            var reader = CreateCachingReader();
            Assert.Throws<InvalidOperationException>(() => action(reader));
        }

        private static void Action_throws_if_called_after_all_rows_read(Action<CachingReader> action)
        {
            var reader = CreateCachingReader();
            bool read = reader.Read();
            Assert.False(read);
            Assert.Throws<InvalidOperationException>(() => action(reader));
        }

        private static CachingReader CreateCachingReader(int rows = 0)
        {
            return new CachingReader(
                new CachedResults(
                    new ColumnMetadata[rows], new List<object[]>(GetRows(rows)), 0));
        }

        private static IEnumerable<object[]> GetRows(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var row = new object[12];
                row[0] = i;
                row[1] = null;
                row[2] = i.ToString();
                row[3] = true;
                row[4] = long.MaxValue;
                row[5] = short.MaxValue;
                row[6] = new Guid("3CC55B83-2DE5-4DCE-9D31-1D306A82972A");
                row[7] = 3.5f;
                row[8] = 5.5d;
                row[9] = 21m;
                row[10] = new DateTime(2001, 11, 17);
                row[11] = (byte)15;

                yield return row;
            }
        }
    }
}