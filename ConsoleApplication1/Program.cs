using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iveonik.Stemmers;
using NSoup.Nodes;
using RestSharp;
using System.Web.Script.Serialization;

using System.Text.RegularExpressions;


namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            FrenchStemmer leStemm = new FrenchStemmer();

            Console.WriteLine("sarkozy:" + leStemm.Stem("sarkozy"));
            Console.WriteLine("hollande:" + leStemm.Stem("hollande"));

            Console.WriteLine("Publique:" + leStemm.Stem("Publique"));
          
           
      

            bool Continue = true;
            int shift = 0;

            do
            {
                StringBuilder Build = new StringBuilder();
                RestClient client = new RestClient("http://192.168.0.12:9200");
                RestRequest request = new RestRequest("indigo3/articles/_bulk", Method.POST);
                List<T_ARTICLES> lesArticles = null;
                string indexLine = "{ \"index\" : {_id:__id__}}";

                using (DBEntities context = new DBEntities())
                {
                    lesArticles =
                        context.T_ARTICLES.Include("TP_PAYS")
                            .Where(p => p.ART_B_VALIDE.Value && p.SER_S_ID.StartsWith("BE"))
                            .OrderByDescending(p => p.ART_DT_PUBLICATION_WEB)
                            .Skip(shift*1000)
                            .Take(1000)
                            .ToList();

                    if (lesArticles.Count() == 0) Continue = false;


                    //Console.WriteLine("Recup EF OK");


                    foreach (T_ARTICLES unArticle in lesArticles)
                    {
                        Build.AppendLine(indexLine.Replace("__id__", unArticle.DOC_I_ID.ToString()));
                        string json = new JavaScriptSerializer().Serialize((new Articles(unArticle)));
                        Build.AppendLine(json);
                    }
                    //Console.WriteLine("Transfo OK");
                }
                Build.AppendLine("");



                //request.AddBody(Build.ToString()); // adds to POST or URL querystring based on Method

                request.AddParameter("text/json", Build.ToString(), ParameterType.RequestBody);


                RestResponse response = (RestResponse) client.Execute(request);
                

                Console.WriteLine("("+response.StatusCode+") - Import de " + (++shift)*1000);

            } while (Continue);



            
            /*Console.WriteLine(content);*/
            Console.ReadKey();



        }
    }

    public class Articles
    {
        public string Titre { get; set; }
        public string Texte { get; set; }
        public string DatePublication { get; set; }

        public string Publication { get; set; }

        public string[] Pays { get; set; } 

        public decimal _id { get; set; }


        public string Langue { get; set; }

        public string CHAPO { get; set; }

        public string[] CanalAbo { get; set; }

        public string Rubrique { get; set; }

        public Articles(T_ARTICLES unArticle)
        {

            Document doc = NSoup.NSoupClient.Parse(unArticle.ART_S_TEXTE);



            String text = doc.Body.Text(); // "An example link"

            
            //var regIllustration = new RegExp("\{\{[a-z0-9]*\}\} ([0-9]*) \{\{/[a-z0-9]*\}\}", "g");

             Regex regexDebut = new Regex("{{[a-z0-9]*}}", RegexOptions.IgnoreCase);
             Regex regexFin = new Regex("{{/[a-z0-9]*}}", RegexOptions.IgnoreCase);
             text = regexDebut.Replace(text, "");
             text = regexFin.Replace(text, "");


            Titre = unArticle.ART_S_TITRE;
            Texte =  text;
            DatePublication = String.Format("{0:s}", unArticle.ART_DT_PUBLICATION); 
            Publication = unArticle.PUB_S_ID;
            Pays = unArticle.TP_PAYS.Select(p => p.PAY_S_FR_LIBELLE).ToArray();
    
            Langue = unArticle.ART_S_LANGUE;
            CHAPO = unArticle.ART_S_CHAPEAU;
           if(unArticle.ART_S_CANAUX!=null)                CanalAbo = unArticle.ART_S_CANAUX.Split(';');
           else CanalAbo=new string [0];
           
            
            Rubrique = unArticle.ART_S_RUBRIQUES;

            _id = unArticle.DOC_I_ID;

        }

    }
}
