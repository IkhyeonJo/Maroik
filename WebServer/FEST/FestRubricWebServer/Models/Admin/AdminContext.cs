using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.Admin
{
    public class AdminContext
    {
        #region 2018년도 FEST
        //public string ConnectionString { get; set; }

        //public AdminContext(string ConnectionString)
        //{
        //    this.ConnectionString = ConnectionString;
        //}

        //private MySqlConnection GetConnection()
        //{
        //    return new MySqlConnection(ConnectionString);
        //}

        //public int IsRegistered(AdminLoginInputModel adminLoginInputModel)
        //{
        //    string ID = null;
        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = string.Format("SELECT {0} FROM {1} where {2}='{3}' AND {4} = password('{5}')"
        //            , nameof(adminLoginInputModel.ID)
        //            , nameof(TableNames.Users)
        //            , nameof(adminLoginInputModel.ID)
        //            , adminLoginInputModel.ID
        //            , nameof(adminLoginInputModel.Password)
        //            , adminLoginInputModel.Password);

        //        var cmd = new MySqlCommand(query, conn);
        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                ID = reader[nameof(adminLoginInputModel.ID)].ToString();
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return ID == null ? 0 : 1;
        //}

        //public AdminLoginInputModel GetAdmin(AdminLoginInputModel adminLoginInputModel)
        //{
        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();

        //        string query = string.Format("SELECT * FROM {0} where {1}='{2}'"
        //            , nameof(TableNames.Users)
        //            , nameof(adminLoginInputModel.ID)
        //            , adminLoginInputModel.ID);

        //        var cmd = new MySqlCommand(query, conn);
        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                adminLoginInputModel.ID = reader[nameof(adminLoginInputModel.ID)].ToString();
        //                adminLoginInputModel.Password = reader[nameof(adminLoginInputModel.Password)].ToString();
        //                adminLoginInputModel.Category = reader[nameof(adminLoginInputModel.Category)].ToString();
        //                adminLoginInputModel.Level = Convert.ToInt32(reader[nameof(adminLoginInputModel.Level)]);
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return adminLoginInputModel;
        //}

        //public List<TeamParticipationOutputModel> GetAllTeamParticipation(string Category)
        //{
        //    List<TeamParticipationOutputModel> list = new List<TeamParticipationOutputModel>();

        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = string.Format("SELECT * FROM {0} WHERE {1}='{2}' ORDER BY {3} DESC, {4} ASC"
        //            , nameof(TableNames.TeamParticipation)
        //            , nameof(TeamParticipationOutputModel.Category)
        //            , Category
        //            , nameof(TeamParticipationOutputModel.KRC)
        //            , nameof(TeamParticipationOutputModel.TeamNo));
        //        var cmd = new MySqlCommand(query, conn);

        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                list.Add(new TeamParticipationOutputModel()
        //                {
        //                    Category = reader[nameof(TeamParticipationOutputModel.Category)].ToString(),
        //                    Year = reader[nameof(TeamParticipationOutputModel.Year)].ToString(),
        //                    TeamNo = Convert.ToInt32(reader[nameof(TeamParticipationOutputModel.TeamNo)]),
        //                    TeamName = reader[nameof(TeamParticipationOutputModel.TeamName)].ToString(),
        //                    CoachName = reader[nameof(TeamParticipationOutputModel.CoachName)].ToString(),
        //                    CoachPhone = reader[nameof(TeamParticipationOutputModel.CoachPhone)].ToString(),
        //                    CoachEmail = reader[nameof(TeamParticipationOutputModel.CoachEmail)].ToString(),
        //                    TeamOrganization = reader[nameof(TeamParticipationOutputModel.TeamOrganization)].ToString(),
        //                    CoachPassword = reader[nameof(TeamParticipationOutputModel.CoachPassword)].ToString(),
        //                    KRC = reader[nameof(TeamParticipationOutputModel.KRC)].ToString()
        //                });
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return list;
        //}

        //public int AdminKRCSave(TeamParticipationOutputModel teamParticipationOutputModels, string KRC)
        //{
        //    int rowCount = 0;
        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = {4}"
        //            , nameof(TableNames.TeamParticipation)
        //            , nameof(TeamParticipationOutputModel.KRC)
        //            , KRC
        //            , nameof(TeamParticipationOutputModel.TeamNo)
        //            , teamParticipationOutputModels.TeamNo);
        //        var cmd = new MySqlCommand(query, conn);
        //        rowCount = cmd.ExecuteNonQuery();
        //        conn.Close();
        //    }
        //    return rowCount;
        //}


        //public List<MemberOutputModel> GetAllMember(string TeamNo)
        //{
        //    List<MemberOutputModel> list = new List<MemberOutputModel>();

        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = string.Format("SELECT * FROM {0} WHERE {1}={2} ORDER BY {3} ASC"
        //            , nameof(TableNames.Member)
        //            , nameof(MemberOutputModel.TeamNo)
        //            , Convert.ToInt32(TeamNo)
        //            , nameof(MemberOutputModel.MemberID));
        //        var cmd = new MySqlCommand(query, conn);

        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                list.Add(new MemberOutputModel()
        //                {
        //                    MemberID = Convert.ToInt32(reader[nameof(MemberOutputModel.MemberID)]),
        //                    MemberName = reader[nameof(MemberOutputModel.MemberName)].ToString(),
        //                    ResidentNumber = reader[nameof(MemberOutputModel.ResidentNumber)].ToString(),
        //                    School = reader[nameof(MemberOutputModel.School)].ToString(),
        //                    Level = reader[nameof(MemberOutputModel.Level)].ToString(),
        //                    Grade = reader[nameof(MemberOutputModel.Grade)].ToString(),
        //                    MemberPhone = reader[nameof(MemberOutputModel.MemberPhone)].ToString(),
        //                    MemberEmail = reader[nameof(MemberOutputModel.MemberEmail)].ToString(),
        //                    Category = reader[nameof(MemberOutputModel.Category)].ToString(),
        //                    Year = reader[nameof(MemberOutputModel.Year)].ToString(),
        //                    TeamNo = Convert.ToInt32(reader[nameof(MemberOutputModel.TeamNo)]),
        //                    GuardianName1 = reader[nameof(MemberOutputModel.GuardianName1)].ToString(),
        //                    GuardianPhone1 = reader[nameof(MemberOutputModel.GuardianPhone1)].ToString(),
        //                    Relationship1 = reader[nameof(MemberOutputModel.Relationship1)].ToString()
        //                });
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return list;
        //}


        //public List<InsuranceOutputModel> GetInsurance(string Category)
        //{
        //    List<InsuranceOutputModel> list = new List<InsuranceOutputModel>();

        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();

        //        string query = string.Format
        //            (@"
        //                SELECT 
        //                B.TeamNo,
        //                B.TeamName,
        //                A.MemberName,
        //                A.ResidentNumber
        //                FROM Member AS A 
        //                INNER JOIN TeamParticipation AS B
        //                ON A.TeamNo=B.TeamNo 
        //                WHERE A.Category='{0}'
        //                ORDER BY B.TeamNo;
        //            ", Category);

        //        var cmd = new MySqlCommand(query, conn);

        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                list.Add(new InsuranceOutputModel()
        //                {
        //                    TeamNo = Convert.ToInt32(reader[nameof(InsuranceOutputModel.TeamNo)]),
        //                    TeamName = reader[nameof(InsuranceOutputModel.TeamName)].ToString(),
        //                    MemberName = reader[nameof(InsuranceOutputModel.MemberName)].ToString(),
        //                    ResidentNumber = reader[nameof(InsuranceOutputModel.ResidentNumber)].ToString()
        //                });
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return list;
        //}

        //public List<ProgramBookOutputModel> GetProgramBook(string Category)
        //{
        //    List<ProgramBookOutputModel> list = new List<ProgramBookOutputModel>();

        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();

        //        string query = string.Format
        //            (@"
        //                SELECT 
        //                B.TeamNo,
        //                B.TeamName,
        //                B.CoachName,
        //                A.MemberName
        //                FROM Member AS A 
        //                INNER JOIN TeamParticipation AS B
        //                ON A.TeamNo=B.TeamNo 
        //                WHERE A.Category='{0}'
        //                ORDER BY B.TeamNo;
        //            ", Category);

        //        var cmd = new MySqlCommand(query, conn);

        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                list.Add(new ProgramBookOutputModel()
        //                {
        //                    TeamNo = Convert.ToInt32(reader[nameof(ProgramBookOutputModel.TeamNo)]),
        //                    TeamName = reader[nameof(ProgramBookOutputModel.TeamName)].ToString(),
        //                    CoachName = reader[nameof(ProgramBookOutputModel.CoachName)].ToString(),
        //                    MemberName = reader[nameof(ProgramBookOutputModel.MemberName)].ToString()
        //                });
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return list;
        //}

        //public List<MemberOutputModel> GetCurrentAllRegisteredMember(string Category)
        //{
        //    List<MemberOutputModel> list = new List<MemberOutputModel>();

        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = string.Format("SELECT * FROM {0} WHERE {1}='{2}' ORDER BY {3} ASC"
        //            , nameof(TableNames.Member)
        //            , nameof(MemberOutputModel.Category)
        //            , Category
        //            , nameof(MemberOutputModel.TeamNo));
        //        var cmd = new MySqlCommand(query, conn);

        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                list.Add(new MemberOutputModel()
        //                {
        //                    MemberID = Convert.ToInt32(reader[nameof(MemberOutputModel.MemberID)]),
        //                    MemberName = reader[nameof(MemberOutputModel.MemberName)].ToString(),
        //                    ResidentNumber = reader[nameof(MemberOutputModel.ResidentNumber)].ToString(),
        //                    School = reader[nameof(MemberOutputModel.School)].ToString(),
        //                    Level = reader[nameof(MemberOutputModel.Level)].ToString(),
        //                    Grade = reader[nameof(MemberOutputModel.Grade)].ToString(),
        //                    MemberPhone = reader[nameof(MemberOutputModel.MemberPhone)].ToString(),
        //                    MemberEmail = reader[nameof(MemberOutputModel.MemberEmail)].ToString(),
        //                    Category = reader[nameof(MemberOutputModel.Category)].ToString(),
        //                    Year = reader[nameof(MemberOutputModel.Year)].ToString(),
        //                    TeamNo = Convert.ToInt32(reader[nameof(MemberOutputModel.TeamNo)]),
        //                    GuardianName1 = reader[nameof(MemberOutputModel.GuardianName1)].ToString(),
        //                    GuardianPhone1 = reader[nameof(MemberOutputModel.GuardianPhone1)].ToString(),
        //                    Relationship1 = reader[nameof(MemberOutputModel.Relationship1)].ToString()
        //                });
        //            }
        //        }
        //        conn.Close();
        //    }
        //    return list;
        //}
        #endregion

        #region 2019년도 FEST
        public string ConnectionString { get; set; }

        public AdminContext(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public int IsRegistered(AdminLoginInputModel adminLoginInputModel)
        {
            string ID = null;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = "SELECT ID FROM login WHERE ID = @ID AND Password = @Password";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", adminLoginInputModel.ID);
                    cmd.Parameters.AddWithValue("@Password", adminLoginInputModel.Password);
                    cmd.Prepare(); //Prepared Statement
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ID = reader["ID"].ToString();
                        }
                    }
                    conn.Close();
                }
            }
            return ID == null ? 0 : 1;
        }

        public AdminLoginInputModel GetAdmin(AdminLoginInputModel adminLoginInputModel)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM login WHERE ID = @ID";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", adminLoginInputModel.ID);
                    cmd.Prepare(); //Prepared Statement
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            adminLoginInputModel.ID = reader["ID"].ToString();
                            adminLoginInputModel.Password = reader["Password"].ToString();
                            adminLoginInputModel.Category = reader["Category"].ToString();
                            adminLoginInputModel.Type = reader["Type"].ToString();
                            adminLoginInputModel.Reserved0 = reader["Reserved0"].ToString();
                            adminLoginInputModel.Reserved1 = reader["Reserved1"].ToString();
                            adminLoginInputModel.Reserved2 = reader["Reserved2"].ToString();
                            adminLoginInputModel.Reserved3 = reader["Reserved3"].ToString();
                            adminLoginInputModel.Reserved4 = reader["Reserved4"].ToString();
                        }
                    }
                }
                conn.Close();
            }
            return adminLoginInputModel;
        }

        public List<AdminRubric> GetAllRubric(string tableName, string type)
        {
            List<AdminRubric> list = new List<AdminRubric>();

            using (var conn = GetConnection())
            {
                conn.Open();

                string query = $"SELECT * FROM {tableName} WHERE Type = '{type}' ORDER BY TeamNumber ASC";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AdminRubric()
                            {
                                TeamNumber = Convert.ToInt32(reader["TeamNumber"]),
                                Evaluator = reader["Evaluator"].ToString(),
                                Type = reader["Type"].ToString(),
                                CategoryOne0 = Convert.ToInt32(reader["CategoryOne0"]),
                                CategoryOne1 = Convert.ToInt32(reader["CategoryOne1"]),
                                CategoryOne2 = Convert.ToInt32(reader["CategoryOne2"]),
                                CategoryTwo0 = Convert.ToInt32(reader["CategoryTwo0"]),
                                CategoryTwo1 = Convert.ToInt32(reader["CategoryTwo1"]),
                                CategoryTwo2 = Convert.ToInt32(reader["CategoryTwo2"]),
                                CategoryThree0 = Convert.ToInt32(reader["CategoryThree0"]),
                                CategoryThree1 = Convert.ToInt32(reader["CategoryThree1"]),
                                CategoryThree2 = Convert.ToInt32(reader["CategoryThree2"]),
                                CategoryScore = Convert.ToInt32(reader["CategoryScore"]),
                                GoodText = reader["GoodText"].ToString(),
                                Opinion = reader["Opinion"].ToString(),
                                Reserved0 = reader["Reserved0"].ToString(),
                                Reserved1 = reader["Reserved1"].ToString(),
                                Reserved2 = reader["Reserved2"].ToString(),
                                Reserved3 = reader["Reserved3"].ToString(),
                                Reserved4 = reader["Reserved4"].ToString()
                            });
                        }
                    }
                }
                conn.Close();
            }
            return list;
        }

        public int CreateRubric(AdminRubric adminRubric, string tableName, string type)
        {
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = $"INSERT INTO {tableName} (TeamNumber, Evaluator, Type, CategoryOne0, CategoryOne1, CategoryOne2, CategoryTwo0, CategoryTwo1, CategoryTwo2, CategoryThree0, CategoryThree1, CategoryThree2, CategoryScore, GoodText, Opinion) VALUES (@TeamNumber, @Evaluator, @Type, @CategoryOne0, @CategoryOne1, @CategoryOne2, @CategoryTwo0, @CategoryTwo1, @CategoryTwo2, @CategoryThree0, @CategoryThree1, @CategoryThree2, @CategoryScore, @GoodText, @Opinion)";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamNumber", adminRubric.TeamNumber);
                cmd.Parameters.AddWithValue("@Evaluator", adminRubric.Evaluator);
                cmd.Parameters.AddWithValue("@Type", type);
                cmd.Parameters.AddWithValue("@CategoryOne0", adminRubric.CategoryOne0);
                cmd.Parameters.AddWithValue("@CategoryOne1", adminRubric.CategoryOne1);
                cmd.Parameters.AddWithValue("@CategoryOne2", adminRubric.CategoryOne2);
                cmd.Parameters.AddWithValue("@CategoryTwo0", adminRubric.CategoryTwo0);
                cmd.Parameters.AddWithValue("@CategoryTwo1", adminRubric.CategoryTwo1);
                cmd.Parameters.AddWithValue("@CategoryTwo2", adminRubric.CategoryTwo2);
                cmd.Parameters.AddWithValue("@CategoryThree0", adminRubric.CategoryThree0);
                cmd.Parameters.AddWithValue("@CategoryThree1", adminRubric.CategoryThree1);
                cmd.Parameters.AddWithValue("@CategoryThree2", adminRubric.CategoryThree2);
                cmd.Parameters.AddWithValue("@CategoryScore", adminRubric.CategoryScore);
                cmd.Parameters.AddWithValue("@GoodText", adminRubric.GoodText);
                cmd.Parameters.AddWithValue("@Opinion", adminRubric.Opinion);
                cmd.Prepare(); //Prepared Statement
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public List<AdminRubric> GetRubric(string tableName, int? teamNumber)
        {
            List<AdminRubric> list = new List<AdminRubric>();

            using (var conn = GetConnection())
            {
                conn.Open();

                string query = $"SELECT * FROM {tableName} WHERE TeamNumber = @TeamNumber ORDER BY TeamNumber ASC";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TeamNumber", teamNumber);
                    cmd.Prepare(); //Prepared Statement
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AdminRubric()
                            {
                                TeamNumber = Convert.ToInt32(reader["TeamNumber"]),
                                Evaluator = reader["Evaluator"].ToString(),
                                Type = reader["Type"].ToString(),
                                CategoryOne0 = Convert.ToInt32(reader["CategoryOne0"]),
                                CategoryOne1 = Convert.ToInt32(reader["CategoryOne1"]),
                                CategoryOne2 = Convert.ToInt32(reader["CategoryOne2"]),
                                CategoryTwo0 = Convert.ToInt32(reader["CategoryTwo0"]),
                                CategoryTwo1 = Convert.ToInt32(reader["CategoryTwo1"]),
                                CategoryTwo2 = Convert.ToInt32(reader["CategoryTwo2"]),
                                CategoryThree0 = Convert.ToInt32(reader["CategoryThree0"]),
                                CategoryThree1 = Convert.ToInt32(reader["CategoryThree1"]),
                                CategoryThree2 = Convert.ToInt32(reader["CategoryThree2"]),
                                CategoryScore = Convert.ToInt32(reader["CategoryScore"]),
                                GoodText = reader["GoodText"].ToString(),
                                Opinion = reader["Opinion"].ToString(),
                                Reserved0 = reader["Reserved0"].ToString(),
                                Reserved1 = reader["Reserved1"].ToString(),
                                Reserved2 = reader["Reserved2"].ToString(),
                                Reserved3 = reader["Reserved3"].ToString(),
                                Reserved4 = reader["Reserved4"].ToString()
                            });
                        }
                    }
                }
                conn.Close();
            }
            return list;
        }

        public int UpdateRubric(AdminRubric adminRubric, string tableName, string type)
        {
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = $"UPDATE {tableName} SET TeamNumber=@TeamNumber, Evaluator=@Evaluator, Type=@Type, CategoryOne0=@CategoryOne0, CategoryOne1=@CategoryOne1, CategoryOne2=@CategoryOne2, CategoryTwo0=@CategoryTwo0, CategoryTwo1=@CategoryTwo1, CategoryThree2=@CategoryThree2, CategoryScore=@CategoryScore, GoodText=@GoodText, Opinion=@Opinion WHERE TeamNumber=@BeforeTeamNumber";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamNumber", adminRubric.TeamNumber);
                cmd.Parameters.AddWithValue("@Evaluator", adminRubric.Evaluator);
                cmd.Parameters.AddWithValue("@Type", type);
                cmd.Parameters.AddWithValue("@CategoryOne0", adminRubric.CategoryOne0);
                cmd.Parameters.AddWithValue("@CategoryOne1", adminRubric.CategoryOne1);
                cmd.Parameters.AddWithValue("@CategoryOne2", adminRubric.CategoryOne2);
                cmd.Parameters.AddWithValue("@CategoryTwo0", adminRubric.CategoryTwo0);
                cmd.Parameters.AddWithValue("@CategoryTwo1", adminRubric.CategoryTwo1);
                cmd.Parameters.AddWithValue("@CategoryTwo2", adminRubric.CategoryTwo2);
                cmd.Parameters.AddWithValue("@CategoryThree0", adminRubric.CategoryThree0);
                cmd.Parameters.AddWithValue("@CategoryThree1", adminRubric.CategoryThree1);
                cmd.Parameters.AddWithValue("@CategoryThree2", adminRubric.CategoryThree2);
                cmd.Parameters.AddWithValue("@CategoryScore", adminRubric.CategoryScore);
                cmd.Parameters.AddWithValue("@GoodText", adminRubric.GoodText);
                cmd.Parameters.AddWithValue("@Opinion", adminRubric.Opinion);
                cmd.Parameters.AddWithValue("@BeforeTeamNumber", adminRubric.BeforeTeamNumber);
                cmd.Prepare(); //Prepared Statement
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public int DeleteRubric(int? teamNumber, string tableName)
        {
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = $"DELETE FROM {tableName} WHERE TeamNumber = @TeamNumber";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamNumber", teamNumber);
                cmd.Prepare(); //Prepared Statement
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public bool IsAlreadyRubricRegistered(int? teamNumber, string tableName)
        {
            int? tempTeamNumber = null;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = $"SELECT TeamNumber FROM {tableName} WHERE TeamNumber = @TeamNumber";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TeamNumber", teamNumber);
                cmd.Prepare(); //Prepared Statement
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tempTeamNumber = Convert.ToInt32(reader["TeamNumber"]);
                    }
                }
                conn.Close();
            }
            return tempTeamNumber != null ? true : false;
        }
        #endregion
    }
}
