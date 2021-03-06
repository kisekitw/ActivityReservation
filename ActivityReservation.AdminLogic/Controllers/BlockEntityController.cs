﻿using System;
using System.Linq.Expressions;
using ActivityReservation.Business;
using ActivityReservation.Helpers;
using ActivityReservation.Models;
using ActivityReservation.WorkContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeihanLi.AspNetMvc.MvcSimplePager;

namespace ActivityReservation.AdminLogic.Controllers
{
    /// <summary>
    /// 黑名单
    /// </summary>
    public class BlockEntityController : AdminBaseController
    {
        // GET: Admin/BlockEntity
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult BlockTypes()
        {
            return Json(HttpContext.RequestServices.GetService<IBLLBlockType>().Select(_ => true));
        }

        /// <summary>
        /// 数据视图
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public ActionResult List(SearchHelperModel search)
        {
            //默认查询全部
            Expression<Func<BlockEntity, bool>> whereLambda = (b => true);
            //判断查询条件
            if (!string.IsNullOrEmpty(search.SearchItem1) && !("0".Equals(search.SearchItem1)))
            {
                var id = Guid.Parse(search.SearchItem1);
                if (!string.IsNullOrEmpty(search.SearchItem2))
                {
                    whereLambda = (b => b.BlockTypeId == id && b.BlockValue.Contains(search.SearchItem2));
                }
                else
                {
                    whereLambda = (b => b.BlockTypeId == id);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(search.SearchItem2))
                {
                    whereLambda = (b => b.BlockValue.Contains(search.SearchItem2));
                }
            }
            try
            {
                var blockList = _blockEntityHelper.Paged(search.PageIndex, search.PageSize, whereLambda, b => b.BlockTime, false);
                var dataList = blockList.ToPagedList(search.PageIndex, search.PageSize, blockList.TotalCount);
                return View(dataList);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// 添加到黑名单
        /// </summary>
        /// <param name="model">黑名单model</param>
        /// <returns></returns>
        public ActionResult AddEntity(Guid typeId, string blockValue)
        {
            if (ModelState.IsValid)
            {
                var entity = new BlockEntity()
                {
                    BlockId = Guid.NewGuid(),
                    BlockTypeId = typeId,
                    BlockValue = blockValue,
                    BlockTime = DateTime.Now
                };
                try
                {
                    var count = _blockEntityHelper.Insert(entity);
                    if (count == 1)
                    {
                        //记录日志
                        OperLogHelper.AddOperLog(string.Format("添加 {0} 到黑名单", blockValue), OperLogModule.BlockEntity,
                            Username);
                        return Json(true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                return Json(false);
            }
            else
            {
                return Json(false);
            }
        }

        /// <summary>
        /// 更新黑名单实体状态
        /// </summary>
        /// <param name="entityId">黑名单数据id</param>
        /// <param name="status">状态</param>
        /// <returns></returns>
        public ActionResult UpdateEntityStatus(Guid entityId, string entityName, int status)
        {
            try
            {
                var count = _blockEntityHelper.Update(e => e.BlockId == entityId, e => e.IsActive, status > 0);
                if (count > 0)
                {
                    OperLogHelper.AddOperLog(
                        string.Format("更改黑名单 {0} 状态为 {1}", entityName, status > 0 ? "启用" : "禁用"),
                        OperLogModule.BlockEntity, Username);
                    return Json(true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return Json(false);
        }

        /// <summary>
        /// 从黑名单中删除一条数据
        /// </summary>
        /// <param name="entityId">黑名单数据id</param>
        /// <param name="entityName">黑名单数据名称</param>
        /// <returns></returns>
        public ActionResult DeleteEntity(Guid entityId, string entityName)
        {
            try
            {
                var c = _blockEntityHelper.Delete(b => b.BlockId == entityId);
                if (c == 1)
                {
                    //记录日志
                    OperLogHelper.AddOperLog($"删除黑名单 {entityName}", OperLogModule.BlockEntity, Username);
                    return Json(true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return Json(false);
        }

        private readonly IBLLBlockEntity _blockEntityHelper;

        public BlockEntityController(ILogger<OperationLogController> logger, OperLogHelper operLogHelper, IBLLBlockEntity bLLBlockEntity) : base(logger, operLogHelper)
        {
            _blockEntityHelper = bLLBlockEntity;
        }
    }
}
