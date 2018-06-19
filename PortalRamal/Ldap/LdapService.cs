using PortalRamal.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace PortalRamal.Ldap
{
    public class LdapService
    {
        private string _pathLdap;
        private string UserAdmin;
        private string Password;


        private IEnumerable<SelectListItem> Edificios
        {
            get
            {
                var selectItems = new List<SelectListItem>();
                if (WebConfigurationManager.AppSettings["Edificios"] != null)
                {
                    var edificios = WebConfigurationManager.AppSettings["Edificios"].Split(';');
                    foreach (var item in edificios)
                        selectItems.Add(new SelectListItem { Text = item, Value = item });

                }
                return selectItems;

            }
        }

        private IEnumerable<SelectListItem> Andares
        {
            get
            {
                var selectItems = new List<SelectListItem>();
                if (WebConfigurationManager.AppSettings["Andares"] != null)
                {
                    var edificios = WebConfigurationManager.AppSettings["Andares"].Split(';');
                    foreach (var item in edificios)
                        selectItems.Add(new SelectListItem { Text = item, Value = item });

                }
                return selectItems;
            }
        }


        public LdapService(string pathLdap)
        {
            _pathLdap = pathLdap;
            UserAdmin = WebConfigurationManager.AppSettings["UserDomainAdmin"];
            Password = WebConfigurationManager.AppSettings["Password"];
        }
        public DirectoryEntry GetDirectoryEntry()
        {
            DirectoryEntry de = new DirectoryEntry
            {
                Path = "LDAP://" + _pathLdap,
                Username = UserAdmin,
                Password = Password
            };
            return de;
        }

        /// <summary>
        /// Establish identity (principal) and culture for a thread.
        /// </summary>
        public static void SetCultureAndIdentity()
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            WindowsPrincipal principal = (WindowsPrincipal)Thread.CurrentPrincipal;
            WindowsIdentity identity = (WindowsIdentity)principal.Identity;
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        public bool UserExists(string UserName)
        {
            DirectoryEntry de = GetDirectoryEntry();
            DirectorySearcher deSearch = new DirectorySearcher
            {
                SearchRoot = de,
                Filter = "(&(objectClass=user) (cn=" + UserName + "))"
            };
            SearchResultCollection results = deSearch.FindAll();
            if (results.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Atualiza informa
        /// </summary>
        /// <param name="usuario"></param>
        public void ModifyUser(Usuario usuario)
        {
            DirectoryEntry de = GetDirectoryEntry();
            string userName = ExtractUserName(Thread.CurrentPrincipal.Identity.Name);
            DirectorySearcher ds = new DirectorySearcher(de)
            {
                Filter = "(&(objectCategory=person)(objectClass=user)(sAMAccountName=" + userName + "))",
                SearchScope = SearchScope.Subtree
            };
            SearchResult results = ds.FindOne();
            using (DirectoryEntry l_entryToModify = results.GetDirectoryEntry())
            {
                l_entryToModify.Properties["displayname"].Clear();
                l_entryToModify.Properties["displayname"].Add(usuario.Nome);
                l_entryToModify.Properties["streetaddress"].Clear();
                l_entryToModify.Properties["streetaddress"].Add(usuario.SelectedEdificio);
                l_entryToModify.Properties["telephonenumber"].Clear();
                l_entryToModify.Properties["telephonenumber"].Add(usuario.Ramal);
                l_entryToModify.Properties["department"].Clear();
                l_entryToModify.Properties["department"].Add(usuario.Unidade);
                l_entryToModify.Properties["postofficebox"].Clear();
                l_entryToModify.Properties["postofficebox"].Add(usuario.SelectedAndar);

                l_entryToModify.CommitChanges();
            }
            de.Close();
        }

        /// <summary>
        /// Helper method that sets properties for AD users.
        /// </summary>
        /// <param name="de"></param>
        /// <param name="PropertyName"></param>
        /// <param name="PropertyValue"></param>
        public static void SetProperty(DirectoryEntry de, string PropertyName, string PropertyValue)
        {
            if (PropertyValue != null)
            {
                if (de.Properties.Contains(PropertyName))
                {
                    de.Properties[PropertyName][0] = PropertyValue;
                }
                else
                {
                    de.Properties[PropertyName].Add(PropertyValue);
                }
            }
        }

       
        string ExtractUserName(string path)
        {
            string[] userPath = path.Split(new char[] { '\\' });
            return userPath[userPath.Length - 1];
        }

        private bool IsExistInAD(string loginName)
        {
            string userName = ExtractUserName(loginName);
            DirectorySearcher search = new DirectorySearcher
            {
                Filter = String.Format("(SAMAccountName={0})", userName)
            };
            search.PropertiesToLoad.Add("cn");
            SearchResult result = search.FindOne();

            if (result == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Usuario Search()
        {
            DirectoryEntry directoryEntry = GetDirectoryEntry();//new DirectoryEntry("LDAP://developer.net");
            if (Thread.CurrentPrincipal != null)
            {
                if (IsExistInAD(Thread.CurrentPrincipal.Identity.Name))
                {
                    string userName = ExtractUserName(Thread.CurrentPrincipal.Identity.Name);
                    DirectorySearcher searcher = new DirectorySearcher(directoryEntry)
                    {
                        PageSize = int.MaxValue,
                        Filter = "(&(objectCategory=person)(objectClass=user)(sAMAccountName=" + userName + "))"
                    };

                    foreach (System.DirectoryServices.SearchResult resEnt in searcher.FindAll())
                    {
                        System.DirectoryServices.DirectoryEntry de = resEnt.GetDirectoryEntry();

                        //foreach (DictionaryEntry property in resEnt.Properties)
                        //{
                        //    Debug.Write(property.Key + ": ");
                        //    foreach (var val in (property.Value as ResultPropertyValueCollection))
                        //    {
                        //        Debug.Write(val + "; ");
                        //    }
                        //    Debug.WriteLine("");
                        //}

                        var usuario = new Usuario
                        {
                            Nome = de.Properties["displayname"].Value != null ? de.Properties["displayname"].Value.ToString() : "",
                            SelectedEdificio = de.Properties["streetaddress"].Value != null ? de.Properties["streetaddress"].Value.ToString() : "",
                            SelectedAndar = de.Properties["postofficebox"].Value != null ? de.Properties["postofficebox"].Value.ToString() : "",
                            Andar = Andares,
                            Edificio = Edificios, 
                            Ramal = de.Properties["telephonenumber"].Value != null ? de.Properties["telephonenumber"].Value.ToString() : "",
                            Unidade = de.Properties["department"].Value != null ? de.Properties["department"].Value.ToString() : "",
                        };

                        return usuario;
                    }
                }
            }
            return null;
        }
    }
}