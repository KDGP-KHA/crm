using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Security;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Extensions;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;
using ClosedXML.Excel;
using TSFramework.Libs.Utils;
using Core.Cate.Services;
using Core.Services;
using Core.Sys.Caches.Sys;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class RM_BusinessOpportunityController : AppController
    {
        private readonly RM_BusinessOpportunityCache _businessOpportunityCache;
        private readonly MN_EmployeeCache _employeesCache;
        private readonly RM_CustomerCache _customerCache;
        private readonly Cate_ProductServiceCache _productServiceCache;
        private readonly RM_StatusCache _statusCache;
        private readonly RM_ExchangeHistoryCache _exchangeHistoryCache;
        private readonly RM_SalesTeamMembersCache _salesTeamMembersCache;
        private readonly RM_RolesCache _rolesCache;
        private readonly RM_ContractsCache _contractCache;
        private readonly RM_ProjectCache _projectCache;
        private readonly RM_ProductProjectCache _productProjectCache;
        private readonly RM_ContactPersonsCache _contactPersonsCache;
        private readonly RM_ExchangeHistoryFilePathCache _exchangeHistoryFilePathCache;
        private readonly RM_BusinessOpportunityFilePathCache _businessOpportunityFilePathCache;
        private readonly SysUserCache _userCache;
        private readonly SysUserBoPhanCache _userBoPhanCache;
        private readonly SysUserCache _employeeCache;
        private readonly BusinessOpportunityMailService _businessOpportunityMailService;
        private readonly SMSQueueService _smsQueueService = new SMSQueueService();
        private readonly string _smsTemplateBOUpdateStatus = ConfigurationManager.AppSettings["SMSTemplate_BOUpdateStatus"];
        private readonly string _BusinessOpportunityTitle = AppProcessor.Messagor.GetMessage("BusinessOpportunity_Title");
        private readonly string _ProjectTitle = AppProcessor.Messagor.GetMessage("Project_Title");
        private readonly string _folderImage = ConfigurationManager.AppSettings["AppImageRoot_Path"] ?? "/Contents/File";

        // GET: Cate/RM_BusinessOpportunity
        public RM_BusinessOpportunityController()
        {
            _businessOpportunityCache = new RM_BusinessOpportunityCache();
            _employeesCache = new MN_EmployeeCache();
            _customerCache = new RM_CustomerCache();
            _productServiceCache = new Cate_ProductServiceCache();
            _statusCache = new RM_StatusCache();
            _exchangeHistoryCache = new RM_ExchangeHistoryCache();
            _salesTeamMembersCache = new RM_SalesTeamMembersCache();
            _rolesCache = new RM_RolesCache();
            _contractCache = new RM_ContractsCache();
            _projectCache = new RM_ProjectCache();
            _productProjectCache = new RM_ProductProjectCache();
            _contactPersonsCache = new RM_ContactPersonsCache();
            _userCache = new SysUserCache();
            _userBoPhanCache = new SysUserBoPhanCache();
            _employeeCache = new SysUserCache();
            _exchangeHistoryFilePathCache = new RM_ExchangeHistoryFilePathCache();
            _businessOpportunityFilePathCache = new RM_BusinessOpportunityFilePathCache();
            _businessOpportunityMailService = new BusinessOpportunityMailService();
            _smsQueueService = new SMSQueueService();
        }
        public ActionResult Index(int? id)
        {
            var model = new RM_BusinessOpportunitySearchModel();
            if (id != null)
            {
                model.CustomerID = id.Value;
                var userInfo = _customerCache.GetById(id.Value);
                if (userInfo != null)
                {
                    model.Keyword = userInfo?.CustomerName;
                    model.CustomerNameTitle = userInfo?.CustomerName;
                }
            }
            model.ProductServices = _productServiceCache.GetAll();
            model.Departments = GetAccessibleDepartments();
            model.StatusList = _statusCache.GetStatusBySearchKey("Opportunity");
            return View(model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_BusinessOpportunitySearchModel model)
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);
            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };
            model.UserName = User.UserName;
            var data = _businessOpportunityCache.Get(out var total, model, dataSearch);
            if (data != null)
            {
                foreach (var item in data)
                {
                    item.CanDelete = item.UserCreated == User.UserName;
                }
            }
            if (data != null && data.Count > 0)
            {
                var productServiceList = _productServiceCache.GetAll();
                var employeeList = _employeesCache.GetAll();
                var members = _salesTeamMembersCache.GetAll();
                var roles = _rolesCache.GetAll();

                foreach (var item in data)
                {
                    if (item != null && !string.IsNullOrEmpty(item.ProductServiceIDs))
                    {
                        var idRoles = item.ProductServiceIDs.Split(';')
                             .Select(x => int.Parse(x))
                        .ToList();

                        var roleNames = productServiceList
                            .Where(r => idRoles.Contains(r.pID))
                            .Select(r => r.ShortNameProduct);

                        item.ProductServiceNames = string.Join(";", roleNames);
                    }

                    if (item != null && !string.IsNullOrEmpty(item.EmployeeIDs))
                    {
                        var idRoles = item.EmployeeIDs.Split(';')
                             .Select(x => int.Parse(x))
                        .ToList();

                        item.Members = idRoles.Select(x => new RM_SalesTeamMembersModel
                        {
                            Employee_Name = members.First(n => n.MemberID == x)?.FullName,
                            Roles = roles
                                    .Where(r => members.First(n => n.MemberID == x).RoleID.Split(';')
                                    .Select(int.Parse)
                                    .Contains(r.RoleID))
                                    .ToList(),
                        })
                        .OrderBy(m =>
                            m.Roles.Any(r => r.RoleID == 1) ? 0 :
                            m.Roles.Any(r => r.RoleID == 5) ? 1 : 2)
                        .ToList();
                    }
                }
            }

            var result = Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
            return result;
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ViewContent(int id)
        {
            var model = _businessOpportunityCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_BusinessOpportunityTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            return PartialView("_ViewContent", model);
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add(int? CustomerID)
        {
            var model = new RM_BusinessOpportunityModel();
            if (CustomerID != null)
            {
                model.CustomerID = CustomerID.Value;
                model.CustomerName = _customerCache.GetById(CustomerID.Value).CustomerName;
            }
            model.ListContactPerson = _contactPersonsCache.GetByCustomerID(CustomerID).Select(d => new SelectListItem
            {
                Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                Value = d.ContactPerson_ID.ToString()
            }).ToList();
            model.SPDichVus = _productServiceCache.GetAll();
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(RM_BusinessOpportunityModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CustomerName = _customerCache.GetById(model.CustomerID).CustomerName;
                model.SPDichVus = _productServiceCache.GetAll();
                model.ListContactPerson = _contactPersonsCache.GetByCustomerID(model.CustomerID).Select(d => new SelectListItem
                {
                    Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                    Value = d.ContactPerson_ID.ToString()
                }).ToList();
                return PartialView("_AddCHKD_View", model);
            }

            string response;
            model.SalesStageID = 1;
            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderImage + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuAnh(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }
            if (model.lst_SP != null)
            {
                model.lst_SP = model.lst_SP.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                model.ProductServiceIDs = string.Join(";", model.lst_SP);
            }

            var result = _customerCache.SaveCHKD(model, User.UserName);
            var dataUser = _customerCache.GetById(model.CustomerID);

            if (result == 0)
                response = CreateMessage($"{_BusinessOpportunityTitle} [{dataUser.CustomerName}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_BusinessOpportunityTitle} [{dataUser.CustomerName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_BusinessOpportunityTitle} [{dataUser.CustomerName}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            var model = _businessOpportunityCache.GetById(id);
            var files = _businessOpportunityFilePathCache.GetByBOID(model.BusinessOpportunityID);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_BusinessOpportunityTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            model.SPDichVus = _productServiceCache.GetAll();
            model.ListContactPerson = _contactPersonsCache.GetByCustomerID(model.CustomerID).Select(d => new SelectListItem
            {
                Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                Value = d.ContactPerson_ID.ToString()
            }).ToList();
            if (!string.IsNullOrEmpty(model.ProductServiceIDs))
            {
                model.lst_SP = model.ProductServiceIDs.Split(';').ToList();
            }
            model.ExistingFiles = files.Select(f => new RM_BusinessOpportunityFilePathModel
            {
                FilePathID = f.FilePathID,
                FilePath = f.FilePath
            }).ToList();
            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult DeleteFile(int id)
        {
            var model = _exchangeHistoryFilePathCache.GetById(id);
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{model.FilePath.Split('/').LastOrDefault()}");

            return PartialView("_DeleteFile", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteFile(RM_ExchangeHistoryFilePathModel model)
        {

            if (!string.IsNullOrEmpty(model.FilePath))
            {
                var physicalPath = Server.MapPath(model.FilePath);

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            var deleted = _exchangeHistoryFilePathCache.Delete(model.FilePathID, User.UserName);

            var data = new RM_ExchangeHistoryFormModel();
            data.RM_ExchangeHistory = new RM_ExchangeHistoryModel()
            {
                BusinessOpportunityID = model.BusinessOpportunityID,
                ExchangeDate = DateTime.Now,
            };
            int total;
            var exchangeHistoryFilePath = _exchangeHistoryFilePathCache.Get(
                new RM_ExchangeHistoryFilePathSearchModel()
                {
                    BusinessOpportunityID = model.BusinessOpportunityID
                },
                out total,
                null

            );

            var exchangeHistorys = _exchangeHistoryCache.Get(new RM_ExchangeHistorySearchModel()
            {
                BusinessOpportunityID = model.BusinessOpportunityID,
            }, out total, null);

            if (exchangeHistorys != null && exchangeHistorys.Count > 0)
            {
                var index = 1;
                foreach (var item in exchangeHistorys)
                {
                    item.Index = index.ToString();
                    item.ExchangeHistoryFilePath = exchangeHistoryFilePath.Where(x => x.ExchangeHistoryID == item.ExchangeHistoryID).ToList();
                    index++;
                }
            }

            data.RM_ExchangeHistory.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(model.BusinessOpportunityID, out total, null);
            data.RM_ExchangeHistory.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
            data.RM_ExchangeHistorys = exchangeHistorys;

            var response = CreateMessage($"File",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new
            {
                status = true,
                list = PartialView("_LichSuPhieu", data).RenderToString(),
                message = response
            });
        }


        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditEH(int id)
        {
            var model = _exchangeHistoryCache.GetById(id);
            model.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(model.BusinessOpportunityID, out var total, null);
            model.ListContactPerson = _contactPersonsCache.GetByCustomerID(model.CustomerID).Select(d => new SelectListItem
            {
                Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                Value = d.ContactPerson_ID.ToString()
            }).ToList();
            var files = _exchangeHistoryFilePathCache.GetByEHID(id);
            model.ParticipatingMembersSelect = !string.IsNullOrEmpty(model.ParticipatingMembers) ? model.ParticipatingMembers.Split(';').Select(int.Parse).ToList() : new List<int>();
            model.ExistingFiles = files.Select(f => new RM_ExchangeHistoryFilePathModel
            {
                FilePathID = f.FilePathID,
                FilePath = f.FilePath
            }).ToList();
            model.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
            return PartialView("_EditEH", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult EditEH(RM_ExchangeHistoryModel model)
        {
            if (!ModelState.IsValid)
            {
                var files = _exchangeHistoryFilePathCache.GetByEHID(model.ExchangeHistoryID);
                model.ListContactPerson = _contactPersonsCache.GetByCustomerID(model.CustomerID).Select(d => new SelectListItem
                {
                    Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                    Value = d.ContactPerson_ID.ToString()
                }).ToList();
                model.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(model.BusinessOpportunityID, out var totalEl, null);
                model.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
                model.ParticipatingMembersSelect = !string.IsNullOrEmpty(model.ParticipatingMembers) ? model.ParticipatingMembers.Split(';').Select(int.Parse).ToList() : new List<int>();
                model.ExistingFiles = files.Select(f => new RM_ExchangeHistoryFilePathModel
                {
                    FilePathID = f.FilePathID,
                    FilePath = f.FilePath
                }).ToList();
                return PartialView("_EditExchangeHistory", model);
            }

            if (model.DinhKemFileEdit != null && model.DinhKemFileEdit.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFileEdit)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderImage + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuAnh(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }

            if (model.DeletedFileIds != null && model.DeletedFileIds.Any())
            {
                foreach (var fileId in model.DeletedFileIds)
                {
                    var file = _exchangeHistoryFilePathCache.GetById(fileId);

                    if (file != null && !string.IsNullOrEmpty(file.FilePath))
                    {
                        try
                        {
                            var fullPath = Server.MapPath(file.FilePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppProcessor.Logger.Error(new Exception(ex.ToString()));
                        }
                        _exchangeHistoryFilePathCache.Delete(fileId, User.UserName);
                    }
                }
            }
            model.ParticipatingMembers = model.ParticipatingMembersSelect != null ? string.Join(";", model.ParticipatingMembersSelect) : string.Empty;
            var result = _exchangeHistoryCache.Save(model, User.UserName);

            var data = new RM_ExchangeHistoryFormModel();
            data.RM_ExchangeHistory = new RM_ExchangeHistoryModel()
            {
                BusinessOpportunityID = model.BusinessOpportunityID,
                ExchangeDate = DateTime.Now,
            };
            int total;
            var exchangeHistoryFilePath = _exchangeHistoryFilePathCache.Get(
                new RM_ExchangeHistoryFilePathSearchModel()
                {
                    BusinessOpportunityID = model.BusinessOpportunityID
                },
                out total,
                null

            );

            var exchangeHistorys = _exchangeHistoryCache.Get(new RM_ExchangeHistorySearchModel()
            {
                BusinessOpportunityID = model.BusinessOpportunityID,
            }, out total, null);

            if (exchangeHistorys != null && exchangeHistorys.Count > 0)
            {
                var index = 1;
                foreach (var item in exchangeHistorys)
                {
                    item.Index = index.ToString();
                    item.ExchangeHistoryFilePath = exchangeHistoryFilePath.Where(x => x.ExchangeHistoryID == item.ExchangeHistoryID).ToList();
                    index++;
                }
            }

            data.RM_ExchangeHistory.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(model.BusinessOpportunityID, out total, null);
            data.RM_ExchangeHistory.ListContactPerson = _contactPersonsCache.GetAll().Select(d => new SelectListItem
            {
                Text = d.FullName,
                Value = d.ContactPerson_ID.ToString()
            }).ToList();
            data.RM_ExchangeHistory.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
            data.RM_ExchangeHistorys = exchangeHistorys;
            var response = CreateMessage($"File",
                EnumProcessType.Edit, result > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new
            {
                status = true,
                list = PartialView("_LichSuPhieu", data).RenderToString(),
                message = response
            });
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(RM_BusinessOpportunityModel model)
        {
            if (!ModelState.IsValid)
            {
                var files = _businessOpportunityFilePathCache.GetByBOID(model.BusinessOpportunityID);
                model.SPDichVus = _productServiceCache.GetAll();
                model.ListContactPerson = _contactPersonsCache.GetByCustomerID(model.CustomerID).Select(d => new SelectListItem
                {
                    Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                    Value = d.ContactPerson_ID.ToString()
                }).ToList();
                model.ExistingFiles = files.Select(f => new RM_BusinessOpportunityFilePathModel
                {
                    FilePathID = f.FilePathID,
                    FilePath = f.FilePath
                }).ToList();
                return PartialView("_CHKD_View", model);
            }
            string response;
            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderImage + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuAnh(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }

            if (model.DeletedFileIds != null && model.DeletedFileIds.Any())
            {
                foreach (var fileId in model.DeletedFileIds)
                {
                    var file = _businessOpportunityFilePathCache.GetById(fileId);

                    if (file != null && !string.IsNullOrEmpty(file.FilePath))
                    {
                        try
                        {
                            var fullPath = Server.MapPath(file.FilePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppProcessor.Logger.Error(new Exception(ex.ToString()));
                        }
                        _businessOpportunityFilePathCache.Delete(fileId, User.UserName);
                    }
                }
            }
            if (model.lst_SP != null)
            {
                model.lst_SP = model.lst_SP.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                model.ProductServiceIDs = string.Join(";", model.lst_SP);
            }
            model.SalesStageID = 1;
            var result = _businessOpportunityCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_BusinessOpportunityTitle} [{model.CustomerName}]",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Error);

            else if (result == -9)
                response = CreateMessage($"{_BusinessOpportunityTitle} [{model.CustomerName}]",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_BusinessOpportunityTitle} [{model.CustomerName}]",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Success);
            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _businessOpportunityCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_BusinessOpportunityTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_BusinessOpportunityTitle} [{model.CustomerName}]");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(RM_BusinessOpportunityModel model)
        {
            var deleted = _businessOpportunityCache.Delete(model, User.UserName);

            var response = CreateMessage($"{_BusinessOpportunityTitle} [{model.CustomerName}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult UpdateStatus(int id)
        {
            var model = new RM_ExchangeHistoryFormModel();
            var bomodel = _businessOpportunityCache.GetById(id);
            model.RM_ExchangeHistory = new RM_ExchangeHistoryModel()
            {
                BusinessOpportunityID = id,
                ExchangeDate = DateTime.Now,
                StatusID = bomodel.StatusID
            };
            int total;
            var exchangeHistoryFilePath = _exchangeHistoryFilePathCache.Get(
                new RM_ExchangeHistoryFilePathSearchModel()
                {
                    BusinessOpportunityID = id
                },
                out total,
                null

            );

            var exchangeHistorys = _exchangeHistoryCache.Get(new RM_ExchangeHistorySearchModel()
            {
                BusinessOpportunityID = id,
            }, out total, null);

            if (exchangeHistorys != null && exchangeHistorys.Count > 0)
            {
                var index = 1;
                foreach (var item in exchangeHistorys)
                {
                    item.Index = index.ToString();
                    item.ExchangeHistoryFilePath = exchangeHistoryFilePath.Where(x => x.ExchangeHistoryID == item.ExchangeHistoryID).ToList();
                    index++;
                }
            }

            model.RM_ExchangeHistory.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(id, out total, null);
            model.RM_ExchangeHistory.ListContactPerson = _contactPersonsCache.GetByCustomerID(bomodel.CustomerID).Select(d => new SelectListItem
            {
                Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                Value = d.ContactPerson_ID.ToString()
            }).ToList();
            model.RM_ExchangeHistory.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
            model.RM_ExchangeHistorys = exchangeHistorys;
            return PartialView("_UpdateStatus", model);
        }


        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult UpdateStatus(RM_ExchangeHistoryModel model)
        {
            int totalValid;
            if (!ModelState.IsValid)
            {
                var bomodel = _businessOpportunityCache.GetById(model.BusinessOpportunityID);
                model.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(model.BusinessOpportunityID, out totalValid, null);
                model.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
                model.ListContactPerson = _contactPersonsCache.GetByCustomerID(bomodel.CustomerID).Select(d => new SelectListItem
                {
                    Text = d.FullName + " - " + d.Position + " - " + d.CustomerName,
                    Value = d.ContactPerson_ID.ToString()
                }).ToList();
                return PartialView("_ExchangeHistory", model);
            }
            string response;
            // luu nhieu file
            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderImage + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuAnh(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }
            model.ParticipatingMembers = model.ParticipatingMembersEHSelect != null ? string.Join(";", model.ParticipatingMembersEHSelect) : string.Empty;
            var result = _exchangeHistoryCache.Save(model, User.UserName);

            if (result == 0)
                response = CreateMessage($"{AppProcessor.Messagor.GetMessage("Job_Label_Status")}",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Error);

            else if (result == -9)
                response = CreateMessage($"{AppProcessor.Messagor.GetMessage("Job_Label_Status")}",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);

            else if (result == -8)
                response = CreateMessage($"{AppProcessor.Messagor.GetMessage("Job_Label_Status")}",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);

            else if (result == -7)
                response = CreateMessage($"{AppProcessor.Messagor.GetMessage("Job_Label_Status")}",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);

            else
            {
                response = CreateMessage($"{AppProcessor.Messagor.GetMessage("Job_Label_Status")}", EnumProcessType.Edit, EnumMsgIcon.Success);
                var salesTeamMembers = _salesTeamMembersCache.GetByBusinessOpportunityID(
                    model.BusinessOpportunityID,
                    out totalValid,
                    null);

                var managements = _salesTeamMembersCache.GetManagementByBOID(model.BusinessOpportunityID);

                // To
                var toUserNames = salesTeamMembers
                    .Select(x => x.Employee_Code)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // CC: loại người đã nằm trong danh sách nhận chính,
                // tránh người vừa là quản lý vừa là thành viên nhận trùng thư
                var ccUserNames = managements
                    .Select(x => x.Employee_Code)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Except(toUserNames, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var listUserPhone = salesTeamMembers
                    .Select(x => x.Phone)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                _businessOpportunityMailService.QueueSendUpdateStatusMail(
                    model.BusinessOpportunityID,
                    toUserNames,
                    User.UserName,
                    ccUserNames);

                var opportunity = _businessOpportunityCache.GetById(model.BusinessOpportunityID);
                opportunity.UpdatedBy = User.FullName;

                _smsQueueService.QueueSendByTemplateCode(
                    _smsTemplateBOUpdateStatus,
                    listUserPhone,
                    User.UserName,
                    opportunity);
            }

            //
            var data = new RM_ExchangeHistoryFormModel();
            data.RM_ExchangeHistory = new RM_ExchangeHistoryModel()
            {
                BusinessOpportunityID = model.BusinessOpportunityID,
                ExchangeDate = DateTime.Now,
            };
            int total;
            var exchangeHistoryFilePath = _exchangeHistoryFilePathCache.Get(
                new RM_ExchangeHistoryFilePathSearchModel()
                {
                    BusinessOpportunityID = model.BusinessOpportunityID
                },
                out total,
                null

            );

            var exchangeHistorys = _exchangeHistoryCache.Get(new RM_ExchangeHistorySearchModel()
            {
                BusinessOpportunityID = model.BusinessOpportunityID,
            }, out total, null);

            if (exchangeHistorys != null && exchangeHistorys.Count > 0)
            {
                var index = 1;
                foreach (var item in exchangeHistorys)
                {
                    item.Index = index.ToString();
                    item.ExchangeHistoryFilePath = exchangeHistoryFilePath.Where(x => x.ExchangeHistoryID == item.ExchangeHistoryID).ToList();
                    index++;
                }
            }

            data.RM_ExchangeHistory.Members = _salesTeamMembersCache.GetByBusinessOpportunityID(model.BusinessOpportunityID, out total, null);
            data.RM_ExchangeHistory.ListContactPerson = _contactPersonsCache.GetAll().Select(d => new SelectListItem
            {
                Text = d.FullName,
                Value = d.ContactPerson_ID.ToString()
            }).ToList();
            data.RM_ExchangeHistory.OpportunityStatus = _statusCache.GetStatusBySearchKey("Opportunity");
            data.RM_ExchangeHistorys = exchangeHistorys;
            model.MemberID = 0;
            model.StatusID = 0;
            model.FileAttach = null;
            model.ExchangeContent = string.Empty;
            model.DinhKemFile = null;
            return Json(new
            {
                status = true,
                message = response,
                list = PartialView("_LichSuPhieu", data).RenderToString()
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hàm lưu ảnh
        /// </summary>
        /// <param name="filebase"></param>
        /// <param name="filePath"></param>
        void LuuAnh(HttpPostedFileBase filebase, string filePath)
        {
            if (filebase != null || !string.IsNullOrEmpty(filePath))
            {
                filebase.SaveAs(HostingEnvironment.MapPath(filePath));
            }
        }

        public string RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ConvertProject(int id)
        {
            var opportunity = _businessOpportunityCache.GetById(id);

            if (opportunity == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage($"{_BusinessOpportunityTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            }

            var model = new ConvertProjectViewModel
            {
                BusinessOpportunityID = opportunity.BusinessOpportunityID,

                // ===== Mapping Project =====
                ProjectName = $"{opportunity.OpportunityName} [{opportunity.CustomerName}]",
                CustomerID = opportunity.CustomerID,
                StartDate = DateTime.Now,
                SuccessRate = opportunity.ClosingProbability, // Xác suất chốt

                // ===== Load danh sách =====
                ProductServices = _productServiceCache.GetAll().Select(x =>
                {
                    x.DisplayName = x.DisplayName?.Replace("&nbsp;", "");
                    return x;
                }).ToList(),

                ListContract = _contractCache.GetByCustomerID(opportunity.CustomerID).Select(d => new SelectListItem
                {
                    Text = d.ContractName,
                    Value = d.ContractID.ToString()
                }).ToList()
            };

            // ===== Convert ProductServiceIDs -> ProductProjects =====
            if (!string.IsNullOrEmpty(opportunity.ProductServiceIDs))
            {
                var ids = opportunity.ProductServiceIDs
                    .Split(';')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(int.Parse)
                    .ToList();

                int count = ids.Count;

                model.ProductProjects = ids.Select(x => new RM_ProductProjectModel
                {
                    ProductServiceID = x,
                    ExpectedRevenue = count > 0 ? opportunity.ExpectedValue / count : 0,
                    StartDate = DateTime.Now
                }).ToList();
            }
            else
            {
                // Nếu không có service → tạo 1 dòng trống
                model.ProductProjects.Add(new RM_ProductProjectModel());
            }

            return PartialView("_ConvertProject", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult ConvertProject(ConvertProjectViewModel model)
        {
            if (!string.IsNullOrEmpty(model.SuccessRate.ToString()))
            {
                if (model.SuccessRate < 0 || model.SuccessRate > 100)
                {
                    ModelState.AddModelError("SuccessRate", $"{AppProcessor.Messagor.GetMessage("SuccessRate_Label")} phải từ 0 đến 100.");
                }
            }
            string response;
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                  .SelectMany(v => v.Errors)
                  .Select(e => e.ErrorMessage)
                  .ToList();
                model.ProductServices = _productServiceCache.GetAll().Select(x =>
                {
                    x.DisplayName = x.DisplayName?.Replace("&nbsp;", "");
                    return x;
                }).ToList();

                model.ListContract = _contractCache.GetByCustomerID(model.CustomerID).Select(d => new SelectListItem
                {
                    Text = d.ContractName,
                    Value = d.ContractID.ToString()
                }).ToList();
                return PartialView("_ConvertProjectForm", model);
            }
            try
            {
                // ===== 1. SAVE PROJECT =====
                RM_ProjectModel projectModel = new RM_ProjectModel
                {
                    BusinessOpportunityID = model.BusinessOpportunityID,
                    SuccessRate = model.SuccessRate,
                    ProjectName = model.ProjectName,
                    CustomerID = model.CustomerID,
                    ContractID = model.ContractID,
                    StartDate = model.StartDate
                };

                var projectId = _projectCache.ConvertProject(projectModel, User.UserName);

                if (projectId == -9)
                {
                    response = CreateMessage($"{_ProjectTitle} [{model.ProjectName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
                    return Json(new { status = false, message = response });
                }

                // ===== 2. SAVE PRODUCT PROJECT =====
                List<int> results = new List<int>();

                foreach (var item in model.ProductProjects)
                {
                    var productModel = new RM_ProductProjectModel
                    {
                        ProjectID = projectId,
                        ProductServiceID = item.ProductServiceID,
                        ExpectedRevenue = item.ExpectedRevenue,
                        StartDate = item.StartDate,
                        EndDate = item.EndDate
                    };

                    var rs = _productProjectCache.Convert(productModel, model.BusinessOpportunityID, User.UserName);
                    results.Add(rs);
                }

                // ===== 3. CHECK RESULT =====
                bool hasError = results.Any(x => x <= 0);
                bool hasDuplicate = results.Any(x => x == -9);

                if (hasDuplicate)
                {
                    response = CreateMessage($"kế hoạch thành dự án",
                        EnumProcessType.DataExisted,
                        EnumMsgIcon.Error);
                }
                else if (hasError)
                {
                    response = CreateMessage($"kế hoạch thành dự án",
                        EnumProcessType.Convert,
                        EnumMsgIcon.Error);
                }
                else
                {
                    response = CreateMessage($"kế hoạch thành dự án",
                        EnumProcessType.Convert,
                        EnumMsgIcon.Success);
                }

                return Json(new { status = true, message = response, projectId = projectId });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ViewHistory(int id)
        {
            var model = new RM_ExchangeHistoryFormModel();
            int total;
            var exchangeHistoryFilePath = _exchangeHistoryFilePathCache.Get(
                new RM_ExchangeHistoryFilePathSearchModel()
                {
                    BusinessOpportunityID = id
                },
                out total,
                null

            );

            var exchangeHistorys = _exchangeHistoryCache.Get(new RM_ExchangeHistorySearchModel()
            {
                BusinessOpportunityID = id
            }, out total, null);

            if (exchangeHistorys != null && exchangeHistorys.Count > 0)
            {
                var index = 1;
                foreach (var item in exchangeHistorys)
                {
                    item.Index = index.ToString();
                    item.ExchangeHistoryFilePath = exchangeHistoryFilePath.Where(x => x.ExchangeHistoryID == item.ExchangeHistoryID).ToList();
                    index++;
                }
            }
            model.RM_ExchangeHistorys = exchangeHistorys;

            return PartialView("_ViewHistory", model);
        }

        /// <summary>
        /// Lấy danh sách phòng ban mà user hiện tại được gán quyền xem qua Sys_UserBoPhan_GetByEmail.
        /// </summary>
        private List<MN_BoPhanModel> GetAccessibleDepartments()
        {
            var currentUser = _userCache.GetByUserName(User.UserName);
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.Email))
            {
                return new List<MN_BoPhanModel>();
            }

            return (_userBoPhanCache.GetByEmail(currentUser.Email) ?? new List<MN_BoPhanModel>())
                .GroupBy(x => x.BoPhan_ID)
                .Select(x => x.First())
                .OrderBy(x => x.TenBoPhanView)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách nhân viên theo phòng ban
        /// </summary>
        [HttpGet]
        public JsonResult GetEmployeesByDepartment(int departmentId)
        {
            var employees = _employeeCache.GetByBoPhanAndChucVu(departmentId, null)
                .Select(x => new
                {
                    Value = x.UserId,
                    Text = x.FullName + " (" + x.UserName + ")",
                })
                .ToList();

            return Json(employees, JsonRequestBehavior.AllowGet);
        }

        #region Export

        /// <summary>
        /// Xuất danh sách cơ hội kinh doanh ra file Excel theo điều kiện lọc hiện tại.
        /// Không dùng [AjaxOnly] vì gọi qua window.location.href (không phải Ajax).
        /// </summary>
        [HttpGet]
        public ActionResult Export(string keyword, int? statusID, int? productServiceID,
                                   string fromDate, string toDate, int? customerID)
        {
            RM_BusinessOpportunitySearchModel filter = BuildExportFilter(keyword, statusID, productServiceID, fromDate, toDate, customerID);
            List<RM_BusinessOpportunityModel> data = GetExportData(filter);
            byte[] fileBytes = BuildExportWorkbook(data);
            string fileName = BuildExportFileName(keyword);

            return SendExcelFile(fileBytes, fileName);
        }

        /// <summary>
        /// Bước 1: Dựng search model từ các param truyền vào.
        /// Nhận param riêng lẻ thay vì bind cả SearchModel để tránh lỗi binding do List trong model.
        /// </summary>
        private RM_BusinessOpportunitySearchModel BuildExportFilter(
            string keyword,
            int? statusID,
            int? productServiceID,
            string fromDate,
            string toDate,
            int? customerID)
        {
            return new RM_BusinessOpportunitySearchModel
            {
                Keyword = keyword,
                StatusID = statusID ?? 0,
                ProductServiceID = productServiceID ?? 0,
                FromDate = fromDate,
                ToDate = toDate,
                CustomerID = customerID ?? 0,
                UserName = User.UserName
            };
        }

        /// <summary>
        /// Bước 2: Truy vấn dữ liệu và map tên sản phẩm dịch vụ, tên thành viên.
        /// </summary>
        private List<RM_BusinessOpportunityModel> GetExportData(RM_BusinessOpportunitySearchModel filter)
        {
            BaseSearchModel dataSearch = new BaseSearchModel
            {
                Order = "0",
                OrderDir = "DESC",
                PageSize = -1
            };

            List<RM_BusinessOpportunityModel> data = _businessOpportunityCache.Get(out _, filter, dataSearch);

            if (data == null || data.Count == 0)
            {
                return data;
            }

            List<Cate_ProductServiceModel> productServices = _productServiceCache.GetAll();
            List<RM_SalesTeamMembersModel> members = _salesTeamMembersCache.GetAll();
            List<RM_RolesModel> roles = _rolesCache.GetAll();

            foreach (RM_BusinessOpportunityModel item in data)
            {
                item.ProductServiceNames = MapProductServiceNames(item.ProductServiceIDs, productServices);
                item.EmployeeNames = MapEmployeeNames(item.EmployeeIDs, members, roles);
            }

            return data;
        }

        /// <summary>
        /// Map danh sách ID sản phẩm dịch vụ (chuỗi phân cách ";") sang tên hiển thị.
        /// Dùng p.pID và p.ShortNameProduct để khớp với action Get().
        /// </summary>
        private string MapProductServiceNames(
            string productServiceIDs,
            List<Cate_ProductServiceModel> productServices)
        {
            if (string.IsNullOrEmpty(productServiceIDs))
            {
                return "";
            }

            List<int> ids = productServiceIDs.Split(';').Select(int.Parse).ToList();

            return string.Join(", ", productServices
                .Where(p => ids.Contains(p.pID))
                .Select(p => p.ShortNameProduct));
        }

        /// <summary>
        /// Map danh sách ID nhân viên (chuỗi phân cách ";") sang tên + vai trò.
        /// </summary>
        private string MapEmployeeNames(
            string employeeIDs,
            List<RM_SalesTeamMembersModel> members,
            List<RM_RolesModel> roles)
        {
            if (string.IsNullOrEmpty(employeeIDs))
            {
                return "";
            }

            List<int> ids = employeeIDs.Split(';').Select(int.Parse).ToList();

            return string.Join(", ", ids.Select(id =>
            {
                RM_SalesTeamMembersModel member = members.FirstOrDefault(m => m.MemberID == id);

                if (member == null)
                {
                    return "";
                }

                IEnumerable<string> roleNames = member.RoleID
                    .Split(';')
                    .Select(int.Parse)
                    .Select(rid => roles.FirstOrDefault(r => r.RoleID == rid)?.RoleName)
                    .Where(n => n != null);

                string rolePart = roleNames.Any()
                    ? " (" + string.Join(", ", roleNames) + ")"
                    : "";

                return member.FullName + rolePart;
            }));
        }

        /// <summary>
        /// Bước 3: Tạo file Excel từ dữ liệu đã map.
        /// </summary>
        private byte[] BuildExportWorkbook(List<RM_BusinessOpportunityModel> data)
        {
            byte[] result;

            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add("Co hoi kinh doanh");

                WriteExportHeader(worksheet);
                WriteExportData(worksheet, data);
                FinalizeSheet(worksheet);

                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    result = stream.ToArray();
                }
            }

            return result;
        }

        /// <summary>
        /// Viết dòng tiêu đề cột cho sheet Excel.
        /// Để thay đổi cột: sửa mảng headers và WriteExportData tương ứng.
        /// </summary>
        private void WriteExportHeader(IXLWorksheet worksheet)
        {
            string[] headers = new[]
            {
            "STT",
            "Trạng thái",
            "Mã kế hoạch KD",
            "Tên kế hoạch KD",
            "Tên khách hàng",
            "Sản phẩm dịch vụ",
            "Giá trị dự kiến (triệu)",
            "Xác suất chốt (%)",
            "Thành viên tham gia"
        };

            for (int colIndex = 0; colIndex < headers.Length; colIndex++)
            {
                IXLCell cell = worksheet.Cell(1, colIndex + 1);
                cell.Value = headers[colIndex];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 11;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Row(1).Height = 22;
        }

        /// <summary>
        /// Viết từng dòng dữ liệu vào sheet Excel.
        /// </summary>
        private void WriteExportData(IXLWorksheet worksheet, List<RM_BusinessOpportunityModel> data)
        {
            if (data == null || data.Count == 0) return;

            int stt = 1;

            for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
            {
                RM_BusinessOpportunityModel item = data[rowIndex];
                int excelRow = rowIndex + 2;

                worksheet.Cell(excelRow, 1).Value = stt++;
                worksheet.Cell(excelRow, 2).Value = item.StatusName ?? "";
                worksheet.Cell(excelRow, 3).Value = item.CodeOpportunity ?? "";
                worksheet.Cell(excelRow, 4).Value = item.OpportunityName ?? "";
                worksheet.Cell(excelRow, 5).Value = item.CustomerName ?? "";
                worksheet.Cell(excelRow, 6).Value = item.ProductServiceNames ?? "";
                worksheet.Cell(excelRow, 7).Value = item.ExpectedValue;
                worksheet.Cell(excelRow, 8).Value = item.ClosingProbability;
                worksheet.Cell(excelRow, 9).Value = item.EmployeeNames ?? "";

                ApplyDataRowStyle(worksheet, excelRow, colCount: 9);
            }
        }

        /// <summary>
        /// Áp dụng style cho từng dòng dữ liệu: màu nền xen kẽ, border, format số, căn lề.
        /// Sửa tại đây để thay đổi style toàn bộ file export.
        /// </summary>
        private void ApplyDataRowStyle(IXLWorksheet worksheet, int excelRow, int colCount)
        {
            // Màu nền xen kẽ
            worksheet.Row(excelRow).Style.Fill.BackgroundColor = excelRow % 2 == 0
                ? XLColor.FromHtml("#EBF3FB")
                : XLColor.White;

            // Border toàn dòng
            IXLRange range = worksheet.Range(excelRow, 1, excelRow, colCount);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            // Format số — cột 7 (ExpectedValue) và 8 (ClosingProbability)
            worksheet.Cell(excelRow, 7).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(excelRow, 8).Style.NumberFormat.Format = @"0""%""";

            // Căn lề
            worksheet.Cell(excelRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // STT
            worksheet.Cell(excelRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Trang thai
            worksheet.Cell(excelRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // ← Ma co hoi KD căn giữa
            worksheet.Cell(excelRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;  // Gia tri
            worksheet.Cell(excelRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Xac suat

            // Wrap text cột thành viên — cột 9
            worksheet.Cell(excelRow, 9).Style.Alignment.WrapText = true;
        }

        /// <summary>
        /// Căn chỉnh độ rộng cột và cố định dòng tiêu đề.
        /// </summary>
        private void FinalizeSheet(IXLWorksheet worksheet)
        {
            worksheet.Columns().AdjustToContents();

            foreach (IXLColumn col in worksheet.ColumnsUsed())
            {
                if (col.Width < 10) col.Width = 10;
                if (col.Width > 50) col.Width = 50;
            }

            worksheet.Column(9).Width = 35;
            worksheet.SheetView.FreezeRows(1);
        }

        /// <summary>
        /// Bước 4: Tạo tên file theo keyword và timestamp hiện tại.
        /// </summary>
        private string BuildExportFileName(string keyword)
        {
            string suffix = string.IsNullOrEmpty(keyword)
                ? ""
                : "_" + UtilString.ConvertToUnSign(keyword);

            return "CoHoiKinhDoanh" + suffix + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";
        }

        /// <summary>
        /// Ghi file Excel vào HTTP Response và trả về EmptyResult.
        /// Dùng byte[] + BinaryWrite thay vì File(stream) để tránh stream bị dispose sớm.
        /// </summary>
        private ActionResult SendExcelFile(byte[] fileBytes, string fileName)
        {
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            Response.AddHeader("Content-Length", fileBytes.Length.ToString());
            Response.BinaryWrite(fileBytes);
            Response.Flush();
            Response.End();

            return new EmptyResult();
        }

        #endregion
    }
}