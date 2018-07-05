package com.bobogavin.mapper;

import com.bobogavin.controller.Person;

//com.bobogavin.mapper.PersonMapper, 命名空间必须和xml中定义的一致, 函数名称也必须和定义的一致
public interface PersonMapper {
    Person selectPersonById(Integer id);
}
