using PortalRamal.Ldap;
using PortalRamal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace PortalRamal.Controllers
{
    public class HomeController : Controller
    {
        private LdapService m_LdapService;
        
        public HomeController()
        {
            ViewBag.Path = WebConfigurationManager.AppSettings["LdapPath"];
            if (ViewBag.Path != null)
                m_LdapService = new LdapService(ViewBag.Path);
        }


        public ActionResult Index()
        {
            try
            {
                ViewBag.Error = false;
                var tempData = (MessageVM)TempData["UserMessage"];
                TempData["UserMessage"] = tempData;
                ViewBag.DisplayName = Thread.CurrentPrincipal.Identity.Name;
                if (ViewBag.Path != null)
                {
                    m_LdapService = new LdapService(ViewBag.Path);
                    var usuario = m_LdapService.Search();
                    ViewBag.DisplayName = usuario.Nome;
                    return View(usuario);
                }

                return View();

            }
            catch (Exception ex)
            {
                ViewBag.Error = true;
                TempData["UserMessage"] = new MessageVM() { CssClassName = "alert-error", Title = "Erro", Message = ex.Message };
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return View();
            }
        }
        public ActionResult Update(Usuario usuario)
        {
            try
            {
                TempData["UserMessage"] = new MessageVM() { CssClassName = "alert-sucess", Title = "Sucesso", Message = "Registro Atualizado." };

                m_LdapService.ModifyUser(usuario);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["UserMessage"] = new MessageVM() { CssClassName = "alert-error", Title = "Erro", Message = "Ocorreu um erro ao tentar atualizar os dados." };
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return RedirectToAction("Index");
            }
        }
    }
}