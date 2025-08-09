namespace Maroik.Common.Miscellaneous.Utilities;

public abstract class GuidToken
{
    public static string Generate()
    {
        #region Unique token

        //string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); 

        #endregion

        #region Basic example of creating a unique token containing a time stamp: https: //stackoverflow.com/questions/14643735/how-to-generate-a-unique-token-which-expires-after-24-hours

        var time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
        var key = Guid.NewGuid().ToByteArray();
        var token = Convert.ToBase64String(time.Concat(key).ToArray());

        #endregion

        return token;
    }

    public static bool IsTokenAlive(string token)
    {
        var data = Convert.FromBase64String(token);
        var when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
        return when >= DateTime.UtcNow.AddHours(-24); // how to generate a unique token which expires after 24 hours?
        // token dead
        // token alive 
    }
}