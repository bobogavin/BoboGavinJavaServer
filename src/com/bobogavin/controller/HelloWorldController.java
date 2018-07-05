package com.bobogavin.controller;

import org.apache.ibatis.io.Resources;
import org.apache.ibatis.session.SqlSession;
import org.apache.ibatis.session.SqlSessionFactory;
import org.apache.ibatis.session.SqlSessionFactoryBuilder;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;

import java.io.InputStream;

@Controller
public class HelloWorldController{
    @RequestMapping(value = "/test")
    public String test(){
        FKSqlSessionFactory.selectPersonById(1);
        return "welcome";
    }
}
