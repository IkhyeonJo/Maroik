using FESTRubricWebServer.Models.User;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class UserContext
    {
        public string ConnectionString { get; set; }

        public UserContext(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
        
        public bool IsValidEmailAndTeamNo(RegisterInputModel registerInputModel)
        {
            string coachName;
            bool isValidEmailAndTeamNo = false;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}' AND {4} = {5}"
                    , nameof(registerInputModel.CoachName)
                    , nameof(TableNames.TeamParticipation)
                    , nameof(registerInputModel.CoachEmail)
                    , registerInputModel.CoachEmail
                    , nameof(registerInputModel.TeamNo)
                    , registerInputModel.TeamNo);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coachName = reader[nameof(registerInputModel.CoachName)].ToString();
                        isValidEmailAndTeamNo = !(string.IsNullOrEmpty(coachName));
                    }
                }
                conn.Close();
            }
            return isValidEmailAndTeamNo;
        }

        public bool IsKRCRegistered(RegisterInputModel registerInputModel)
        {
            string coachName;
            bool isKRCRegistered = false;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}' AND {4} = {5} AND {6} = '{7}'"
                    , nameof(registerInputModel.CoachName)
                    , nameof(TableNames.TeamParticipation)
                    , nameof(registerInputModel.CoachEmail)
                    , registerInputModel.CoachEmail
                    , nameof(registerInputModel.TeamNo)
                    , registerInputModel.TeamNo
                    , nameof(registerInputModel.KRC)
                    , "O");
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coachName = reader[nameof(registerInputModel.CoachName)].ToString();
                        isKRCRegistered = !(string.IsNullOrEmpty(coachName));
                    }
                }
                conn.Close();
            }
            return isKRCRegistered;
        }

        public bool IsValidEmailAndTeamNo(ResetPasswordInputModel resetPasswordInputModel)
        {
            string coachName;
            bool isValidEmailAndTeamNo = false;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}' AND {4} = {5}"
                    , nameof(resetPasswordInputModel.CoachName)
                    , nameof(TableNames.TeamParticipation)
                    , nameof(resetPasswordInputModel.CoachEmail)
                    , resetPasswordInputModel.CoachEmail
                    , nameof(resetPasswordInputModel.TeamNo)
                    , resetPasswordInputModel.TeamNo);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coachName = reader[nameof(resetPasswordInputModel.CoachName)].ToString();
                        isValidEmailAndTeamNo = !(string.IsNullOrEmpty(coachName));
                    }
                }
                conn.Close();
            }
            return isValidEmailAndTeamNo;
        }

        public bool IsPasswordExist(RegisterInputModel registerInputModel)
        {
            string coachPassword;
            bool isCoachPasswordExist = false;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}' AND {4} = {5}"
                    , nameof(registerInputModel.CoachPassword)
                    , nameof(TableNames.TeamParticipation)
                    , nameof(registerInputModel.CoachEmail)
                    , registerInputModel.CoachEmail
                    , nameof(registerInputModel.TeamNo)
                    , registerInputModel.TeamNo);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coachPassword = reader[nameof(registerInputModel.CoachPassword)].ToString();
                        isCoachPasswordExist = string.IsNullOrEmpty(coachPassword); //password가 있으면 false를 반환.
                    }
                }
                conn.Close();
            }
            return isCoachPasswordExist;
        }

        public int SetPassword(RegisterInputModel registerInputModel)
        {
            int rowCount=0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}' AND {5} = {6}"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(registerInputModel.CoachPassword)
                    , registerInputModel.CoachPassword
                    , nameof(registerInputModel.CoachEmail)
                    , registerInputModel.CoachEmail
                    , nameof(registerInputModel.TeamNo)
                    , registerInputModel.TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public int ResetPassword(ResetPasswordInputModel resetPasswordInputModel)
        {
            int rowCount = 0;
            string resetPassword = "";
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}' AND {5} = {6}"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(resetPasswordInputModel.CoachPassword)
                    , resetPassword
                    , nameof(resetPasswordInputModel.CoachEmail)
                    , resetPasswordInputModel.CoachEmail
                    , nameof(resetPasswordInputModel.TeamNo)
                    , resetPasswordInputModel.TeamNo);
                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public int IsRegistered(LoginInputModel loginInputModel)
        {
            string coachName = null;
            using (var conn = GetConnection())
            {
                conn.Open();

                //string query = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}' AND {4} = {5} AND {6} = '{7}'"
                //    , nameof(loginInputModel.CoachName)
                //    , nameof(TableNames.TeamParticipation)
                //    , nameof(loginInputModel.CoachEmail)
                //    , loginInputModel.CoachEmail
                //    , nameof(loginInputModel.TeamNo)
                //    , loginInputModel.TeamNo
                //    , nameof(loginInputModel.CoachPassword)
                //    , loginInputModel.CoachPassword);

                string query = $"SELECT {nameof(loginInputModel.CoachName)} FROM {nameof(TableNames.TeamParticipation)} WHERE {nameof(loginInputModel.CoachEmail)} = @{nameof(loginInputModel.CoachEmail)} AND {nameof(loginInputModel.TeamNo)} = @{nameof(loginInputModel.TeamNo)} AND {nameof(loginInputModel.CoachPassword)} = @{nameof(loginInputModel.CoachPassword)}";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue($"@{nameof(loginInputModel.CoachEmail)}", loginInputModel.CoachEmail);
                    cmd.Parameters.AddWithValue($"@{nameof(loginInputModel.TeamNo)}", loginInputModel.TeamNo);
                    cmd.Parameters.AddWithValue($"@{nameof(loginInputModel.CoachPassword)}", loginInputModel.CoachPassword);
                    cmd.Prepare(); //Prepared Statement
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            coachName = reader[nameof(loginInputModel.CoachName)].ToString();
                        }
                    }
                    conn.Close();
                }
            }
            return coachName == null ? 0 : 1;
        }

        public List<string> GetSessionValues(LoginInputModel loginInputModel)
        {
            var sessionValues = new List<string>();
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT {0}, {1} FROM {2} WHERE {3} = {4}"
                    , nameof(LoginInputModel.Category)
                    , nameof(LoginInputModel.Year)
                    , nameof(TableNames.TeamParticipation)
                    , nameof(LoginInputModel.TeamNo)
                    , loginInputModel.TeamNo);
                var cmd = new MySqlCommand(query, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sessionValues.Add(reader[nameof(LoginInputModel.Category)].ToString());
                        sessionValues.Add(Convert.ToInt32(reader[nameof(LoginInputModel.Year)]).ToString());
                    }
                }
                conn.Close();
            }
            return sessionValues;
        }

        public bool IsAgreedContractor(LoginInputModel loginInputModel)
        {
            string personalInformation = null;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}' AND {4} = {5} AND {6} = '{7}'"
                    , nameof(loginInputModel.PersonalInformation)
                    , nameof(TableNames.TeamParticipation)
                    , nameof(loginInputModel.CoachEmail)
                    , loginInputModel.CoachEmail
                    , nameof(loginInputModel.TeamNo)
                    , loginInputModel.TeamNo
                    , nameof(loginInputModel.CoachPassword)
                    , loginInputModel.CoachPassword);
                var cmd = new MySqlCommand(query, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        personalInformation = reader[nameof(loginInputModel.PersonalInformation)].ToString();
                    }
                }
                conn.Close();
            }
            return string.IsNullOrEmpty(personalInformation);
        }

        public int SetPersonalInformation(LoginInputModel loginInputModel)
        {
            loginInputModel.PersonalInformation = "O";
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}' AND {5} = {6}"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(loginInputModel.PersonalInformation)
                    , loginInputModel.PersonalInformation
                    , nameof(loginInputModel.CoachEmail)
                    , loginInputModel.CoachEmail
                    , nameof(loginInputModel.TeamNo)
                    , loginInputModel.TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public int GetRegisteredMemberNumber(string TeamNo)
        {
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = string.Format("SELECT COUNT(*) FROM {0} WHERE {1} = {2}"
                    , nameof(TableNames.Member)
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = Convert.ToInt32(cmd.ExecuteScalar());
                
                conn.Close();
            }
            return rowCount;
        }

        public List<MemberOutputModel> GetAllMember(string TeamNo)
        {
            var list = new List<MemberOutputModel>();

            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.Member)
                    , nameof(TeamNo)
                    , Convert.ToInt32(TeamNo));
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MemberOutputModel()
                        {
                            MemberID = Convert.ToInt32(reader[nameof(MemberOutputModel.MemberID)]),
                            MemberName = reader[nameof(MemberOutputModel.MemberName)].ToString(),
                            ResidentNumber = reader[nameof(MemberOutputModel.ResidentNumber)].ToString(),
                            School = reader[nameof(MemberOutputModel.School)].ToString(),
                            Level = reader[nameof(MemberOutputModel.Level)].ToString(),
                            Grade = Convert.ToInt32(reader[nameof(MemberOutputModel.Grade)]),
                            MemberPhone = reader[nameof(MemberOutputModel.MemberPhone)].ToString(),
                            MemberEmail = reader[nameof(MemberOutputModel.MemberEmail)].ToString(),
                            GuardianName1 = reader[nameof(MemberOutputModel.GuardianName1)].ToString(),
                            GuardianPhone1 = reader[nameof(MemberOutputModel.GuardianPhone1)].ToString(),
                            RelationShip1 = reader[nameof(MemberOutputModel.RelationShip1)].ToString()
                        });
                    }
                }
                conn.Close();
            }
            return list;
        }
        public MemberInputModel GetMember(int? Id)
        {
            var memberOutputModel = new MemberInputModel();

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = string.Format("SELECT * FROM {0} WHERE {1} = {2}"
                    , nameof(TableNames.Member)
                    , nameof(memberOutputModel.MemberID)
                    , Id);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        memberOutputModel.MemberID = Convert.ToInt32(reader[nameof(memberOutputModel.MemberID)]);
                        memberOutputModel.MemberName = reader[nameof(memberOutputModel.MemberName)].ToString();
                        memberOutputModel.ResidentNumber = reader[nameof(MemberInputModel.ResidentNumber)].ToString();
                        memberOutputModel.School = reader[nameof(memberOutputModel.School)].ToString();
                        memberOutputModel.Level = reader[nameof(memberOutputModel.Level)].ToString();
                        memberOutputModel.Grade = Convert.ToInt32(reader[nameof(memberOutputModel.Grade)]);
                        memberOutputModel.MemberPhone = reader[nameof(memberOutputModel.MemberPhone)].ToString();
                        memberOutputModel.MemberEmail = reader[nameof(memberOutputModel.MemberEmail)].ToString();
                        memberOutputModel.GuardianName1 = reader[nameof(MemberInputModel.GuardianName1)].ToString();
                        memberOutputModel.GuardianPhone1 = reader[nameof(MemberInputModel.GuardianPhone1)].ToString();
                        memberOutputModel.RelationShip1 = reader[nameof(MemberInputModel.RelationShip1)].ToString();
                    }
                }
                conn.Close();
            }
            return memberOutputModel;
        }

        public MemberInputModel UpdateMember(MemberInputModel memberInputModel)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                
                string query = string.Format("UPDATE {0} SET {1}='{2}', {3}='{4}', {5}='{6}', {7}='{8}', {9}='{10}', {11}='{12}', {13}='{14}', {15}='{16}', {17}='{18}', {19}='{20}' WHERE {21} = {22}"
                    , nameof(TableNames.Member)
                    , nameof(memberInputModel.MemberName)
                    , memberInputModel.MemberName
                    , nameof(memberInputModel.ResidentNumber)
                    , memberInputModel.ResidentNumber
                    , nameof(memberInputModel.School)
                    , memberInputModel.School
                    , nameof(memberInputModel.Level)
                    , memberInputModel.Level
                    , nameof(memberInputModel.Grade)
                    , memberInputModel.Grade
                    , nameof(memberInputModel.MemberPhone)
                    , memberInputModel.MemberPhone
                    , nameof(memberInputModel.MemberEmail)
                    , memberInputModel.MemberEmail
                    , nameof(memberInputModel.GuardianName1)
                    , memberInputModel.GuardianName1
                    , nameof(memberInputModel.GuardianPhone1)
                    , memberInputModel.GuardianPhone1
                    , nameof(memberInputModel.RelationShip1)
                    , memberInputModel.RelationShip1
                    , nameof(memberInputModel.MemberID)
                    , memberInputModel.MemberID);
                                            
                var cmd = new MySqlCommand(query, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        memberInputModel.MemberID = Convert.ToInt32(reader[nameof(memberInputModel.MemberID)]);
                        memberInputModel.MemberName = reader[nameof(memberInputModel.MemberName)].ToString();
                        memberInputModel.ResidentNumber = reader[nameof(MemberOutputModel.ResidentNumber)].ToString();
                        memberInputModel.School = reader[nameof(memberInputModel.School)].ToString();
                        memberInputModel.Level = reader[nameof(memberInputModel.Level)].ToString();
                        memberInputModel.Grade = Convert.ToInt32(reader[nameof(memberInputModel.Grade)]);
                        memberInputModel.MemberPhone = reader[nameof(memberInputModel.MemberPhone)].ToString();
                        memberInputModel.MemberEmail = reader[nameof(memberInputModel.MemberEmail)].ToString();
                    }
                }
                conn.Close();
            }
            return memberInputModel;
        }

        public int DeleteMember(int? Id)
        {
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("DELETE FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.Member)
                    , nameof(MemberInputModel.MemberID)
                    , Id);
                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public int CreateMember(MemberInputModel memberInputModel, string Category, string Year, string TeamNo)
        {
            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();
                
                string query = string.Format(@"INSERT INTO {0} ({1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}) VALUES ('{14}', '{15}', '{16}', '{17}', '{18}', '{19}','{20}', '{21}', '{22}', '{23}', '{24}', '{25}', '{26}')"
                    , nameof(TableNames.Member)
                    , nameof(memberInputModel.MemberName)
                    , nameof(memberInputModel.ResidentNumber)
                    , nameof(memberInputModel.School)
                    , nameof(memberInputModel.Level)
                    , nameof(memberInputModel.Grade)
                    , nameof(memberInputModel.MemberPhone)
                    , nameof(memberInputModel.MemberEmail)
                    , nameof(Category)
                    , nameof(Year)
                    , nameof(TeamNo)
                    , nameof(memberInputModel.GuardianName1)
                    , nameof(memberInputModel.GuardianPhone1)
                    , nameof(memberInputModel.RelationShip1)
                    , memberInputModel.MemberName
                    , memberInputModel.ResidentNumber
                    , memberInputModel.School
                    , memberInputModel.Level
                    , memberInputModel.Grade
                    , memberInputModel.MemberPhone
                    , memberInputModel.MemberEmail
                    , Category
                    , Year
                    , TeamNo
                    , memberInputModel.GuardianName1
                    , memberInputModel.GuardianPhone1
                    , memberInputModel.RelationShip1);
                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public List<CoachOutputModel> GetCoachInfo(string TeamNo)
        {
            var list = new List<CoachOutputModel>();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(TeamNo)
                    , TeamNo);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CoachOutputModel()
                        {
                            CoachName = reader[nameof(CoachOutputModel.CoachName)].ToString(),
                            CoachPhone = reader[nameof(CoachOutputModel.CoachPhone)].ToString(),
                            CoachEmail = reader[nameof(CoachOutputModel.CoachEmail)].ToString(),
                            TeamOrganization = reader[nameof(CoachOutputModel.TeamOrganization)].ToString()
                        });
                            
                    }
                }
                conn.Close();
            }
            return list;
        }

        public CoachInputModel GetEditCoachInfo(string TeamNo)
        {
            var coachInputModel = new CoachInputModel();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.TeamParticipation) 
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coachInputModel.CoachName = reader[nameof(coachInputModel.CoachName)].ToString();
                        coachInputModel.CoachPhone = reader[nameof(coachInputModel.CoachPhone)].ToString();
                        coachInputModel.CoachEmail = reader[nameof(coachInputModel.CoachEmail)].ToString();
                        coachInputModel.TeamOrganization = reader[nameof(coachInputModel.TeamOrganization)].ToString();
                    }
                }
                conn.Close();
            }
            return coachInputModel;
        }

        public CoachInputModel EditCoachInfo(CoachInputModel coachInputModel, string TeamNo)
        {
            var retCoachInputModel = new CoachInputModel();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("UPDATE {0} SET {1}='{2}', {3}='{4}', {5}='{6}', {7}='{8}' WHERE {9} = '{10}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(coachInputModel.CoachName)
                    , coachInputModel.CoachName
                    , nameof(coachInputModel.CoachPhone)
                    , coachInputModel.CoachPhone
                    , nameof(coachInputModel.CoachEmail)
                    , coachInputModel.CoachEmail
                    , nameof(coachInputModel.TeamOrganization)
                    , coachInputModel.TeamOrganization
                    , nameof(TeamNo)
                    , TeamNo);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        retCoachInputModel.CoachName = reader[nameof(retCoachInputModel.CoachName)].ToString();
                        retCoachInputModel.CoachPhone = reader[nameof(retCoachInputModel.CoachPhone)].ToString();
                        retCoachInputModel.CoachEmail = reader[nameof(retCoachInputModel.CoachEmail)].ToString();
                        retCoachInputModel.TeamOrganization = reader[nameof(retCoachInputModel.TeamOrganization)].ToString();
                    }
                }
                conn.Close();
            }
            return retCoachInputModel;
        }

        public List<TeamOutputModel> GetTeamInfo(string TeamNo)
        {
            var list = new List<TeamOutputModel>();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TeamOutputModel()
                        {
                            Category = reader[nameof(TeamOutputModel.Category)].ToString(),
                            TeamNo = Convert.ToInt32(reader[nameof(TeamOutputModel.TeamNo)]),
                            TeamName = reader[nameof(TeamOutputModel.TeamName)].ToString()
                        });
                    }
                }
                conn.Close();
            }
            return list;
        }

        public TeamInputModel GetEditTeamInfo(string TeamNo)
        {
            var teamInputModel = new TeamInputModel();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        teamInputModel.Category = reader[nameof(teamInputModel.Category)].ToString();
                        teamInputModel.TeamNo = Convert.ToInt32(reader[nameof(teamInputModel.TeamNo)]);
                        teamInputModel.TeamName = reader[nameof(teamInputModel.TeamName)].ToString();
                    }
                }
                conn.Close();
            }
            return teamInputModel;
        }

        public TeamInputModel EditTeamInfo(TeamInputModel teamInputModel)
        {
            var retTeamInputModel = new TeamInputModel();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("UPDATE {0} SET {1}='{2}' WHERE {3} = '{4}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(teamInputModel.TeamName)
                    , teamInputModel.TeamName
                    , nameof(teamInputModel.TeamNo)
                    , teamInputModel.TeamNo);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        retTeamInputModel.Category = reader[nameof(retTeamInputModel.Category)].ToString();
                        retTeamInputModel.TeamNo = Convert.ToInt32(reader[nameof(retTeamInputModel.TeamNo)]);
                        retTeamInputModel.TeamName = reader[nameof(retTeamInputModel.TeamName)].ToString();
                    }
                }
                conn.Close();
            }
            return retTeamInputModel;
        }

        public LoginInputModel GetCategoryAndYear(string TeamNo)
        {
            var loginInputModel = new LoginInputModel();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        loginInputModel.Category = reader[nameof(loginInputModel.Category)].ToString();
                        loginInputModel.Year = reader[nameof(loginInputModel.Year)].ToString();
                    }
                }
                conn.Close();
            }
            return loginInputModel;
        }

        public List<CoachOutputModel> GetAllCategoryCoachInfo(string Category)
        {
            var list = new List<CoachOutputModel>();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}' ORDER BY TeamNo ASC"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(Category)
                    , Category);
                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CoachOutputModel()
                        {
                            CoachName = reader[nameof(CoachOutputModel.CoachName)].ToString(),
                            CoachPhone = reader[nameof(CoachOutputModel.CoachPhone)].ToString(),
                            CoachEmail = reader[nameof(CoachOutputModel.CoachEmail)].ToString(),
                            TeamOrganization = reader[nameof(CoachOutputModel.TeamOrganization)].ToString(),
                            TeamNo = Convert.ToInt32(reader[nameof(CoachOutputModel.TeamNo)])
                        });
                    }
                }
                conn.Close();
            }
            return list;
        }

        public IsNoticeColumnsOutputModel GetIsNoticeColumns(string TeamNo)
        {
            var isNoticeColumnsOutputModel = new IsNoticeColumnsOutputModel();

            using (var conn = GetConnection())
            {
                conn.Open();

                var query = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        isNoticeColumnsOutputModel.IsKRCTeamRegistration = reader[nameof(isNoticeColumnsOutputModel.IsKRCTeamRegistration)].ToString();
                        isNoticeColumnsOutputModel.IsKRCWrittenOath = reader[nameof(isNoticeColumnsOutputModel.IsKRCWrittenOath)].ToString();
                        isNoticeColumnsOutputModel.IsKRCAdmissionTicket = reader[nameof(isNoticeColumnsOutputModel.IsKRCAdmissionTicket)].ToString();
                        isNoticeColumnsOutputModel.IsFTCTeamDocuments = reader[nameof(isNoticeColumnsOutputModel.IsFTCTeamDocuments)].ToString();
                    }
                }
                conn.Close();
            }
            return isNoticeColumnsOutputModel;
        }
        public int SetIsKRCWrittenOath(string TeamNo)
        {
            var isNoticeColumnsInputModel = new IsNoticeColumnsInputModel();
            isNoticeColumnsInputModel.IsKRCWrittenOath = "O";

            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(isNoticeColumnsInputModel.IsKRCWrittenOath)
                    , isNoticeColumnsInputModel.IsKRCWrittenOath
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }

        public int SetIsKRCTeamRegistration(string TeamNo, string IsKRCTeamRegistration)
        {
            var isNoticeColumnsInputModel = new IsNoticeColumnsInputModel();
            isNoticeColumnsInputModel.IsKRCTeamRegistration = IsKRCTeamRegistration;

            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(isNoticeColumnsInputModel.IsKRCTeamRegistration)
                    , isNoticeColumnsInputModel.IsKRCTeamRegistration
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }
        public int SetIsKRCAdmissionTicket(string TeamNo)
        {
            var isNoticeColumnsInputModel = new IsNoticeColumnsInputModel();
            isNoticeColumnsInputModel.IsKRCAdmissionTicket = "O";

            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(isNoticeColumnsInputModel.IsKRCAdmissionTicket)
                    , isNoticeColumnsInputModel.IsKRCAdmissionTicket
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }
        public int SetIsFTCTeamDocuments(string TeamNo)
        {
            var isNoticeColumnsInputModel = new IsNoticeColumnsInputModel();
            isNoticeColumnsInputModel.IsFTCTeamDocuments = "O";

            int rowCount = 0;
            using (var conn = GetConnection())
            {
                conn.Open();

                string query = string.Format("UPDATE {0} SET {1} ='{2}' WHERE {3} = '{4}'"
                    , nameof(TableNames.TeamParticipation)
                    , nameof(isNoticeColumnsInputModel.IsFTCTeamDocuments)
                    , isNoticeColumnsInputModel.IsFTCTeamDocuments
                    , nameof(TeamNo)
                    , TeamNo);

                var cmd = new MySqlCommand(query, conn);
                rowCount = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return rowCount;
        }
    }
}
