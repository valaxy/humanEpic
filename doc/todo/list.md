- 现在可以将scopePanel+canvasView+unassignedPoolView整合到一起了，连接的代码放在 flowtool/Main模块里
    * 左边是scopePanel，中间是canvasView，右边是unassignedPoolView
    * 初始化时scopePanel载入数据，然后选中第一项在canvasView和unassignedPoolView中显示
    * 单击删除按钮