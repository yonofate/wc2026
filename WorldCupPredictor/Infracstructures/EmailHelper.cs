using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace WorldCupPredictor.Infracstructures
{
    public class EmailHelper
    {
        //http://www.technical-recipes.com/2018/how-to-send-an-e-mail-via-google-smtp-using-c/
        public void SendEmail()
        {
            try
            {
                var email = "someEmail@gmail.com";
                var password = "someEmail@gmail.com";
                // Credentials
                var credentials = new NetworkCredential("someEmail@gmail.com", "somePassword");

                // Mail message
                var mail = new MailMessage()
                {
                    From = new MailAddress("someEmail@gmail.com"),
                    Subject = "Test email.",
                    Body = "Test email body"
                };

                mail.To.Add(new MailAddress("someEmail@gmail.com"));

                // Smtp client
                var client = new SmtpClient()
                {
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = "smtp.gmail.com",
                    EnableSsl = true,
                    Credentials = credentials
                };

                // Send it...         
                client.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in sending email: " + ex.Message);
                Console.ReadKey();
                return;
            }
        }
    }
}