package com.bobogavin.controller;

import com.bobogavin.mapper.PersonMapper;
import org.apache.ibatis.io.Resources;
import org.apache.ibatis.session.SqlSession;
import org.apache.ibatis.session.SqlSessionFactory;
import org.apache.ibatis.session.SqlSessionFactoryBuilder;

import java.io.InputStream;

public class FKSqlSessionFactory {
    private static SqlSessionFactory _sqlSessionFactory = null;
    private static SqlSession _sqlSession = null;
    static {
        try{
            InputStream inputStream = Resources.getResourceAsStream("/com/bobogavin/controller/Resources/mybatis-config.xml");
            _sqlSessionFactory = new SqlSessionFactoryBuilder().build(inputStream);
            _sqlSession = _sqlSessionFactory.openSession();
        }
        catch (Exception e){
            System.out.printf(e.getMessage());
        }
    }
    public static void selectPersonById(Integer id){
        try {
            PersonMapper personMapper =_sqlSession.getMapper(PersonMapper.class);
            Person person = personMapper.selectPersonById(id);
            _sqlSession.commit();
            _sqlSession.close();
        }
        catch (Exception e)
        {
            System.out.printf(e.getMessage());
        }

    }
}
